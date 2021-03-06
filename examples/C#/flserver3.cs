﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ZeroMQ;

namespace ZeroMQ.Test
{
	static partial class Program
	{
		public static void FLServer3(IDictionary<string, string> dict, string[] args)
		{
			//
			// Freelance server - Model 3
			// Uses an ROUTER/ROUTER socket but just one thread
			//
			// Authors: Pieter Hintjens, Uli Riehm
			//

			if (args == null || args.Length < 1)
			{
				Console.WriteLine("I: syntax: {0} FLServer3", AppDomain.CurrentDomain.FriendlyName);
			}

			// Prepare server socket with predictable identity
			string bind_endpoint = "tcp://*:5555";
			string connect_endpoint = "tcp://127.0.0.1:5555";

			using (var context = ZContext.Create())
			using (var server = ZSocket.Create(context, ZSocketType.ROUTER))
			{
				Console.CancelKeyPress += (s, ea) =>
				{
					ea.Cancel = true;
					context.Shutdown();
				};

				server.IdentityString = connect_endpoint;
				server.Bind(bind_endpoint);
				Console.WriteLine("I: service is ready as {0}", bind_endpoint);

				ZError error;
				ZMessage request;
				while (true)
				{
					if (null == (request = server.ReceiveMessage(out error)))
					{
						if (error == ZError.ETERM)
							break;	// Interrupted
						throw new ZException(error);
					}
					using (var response = new ZMessage())
					{
						ZFrame identity;

						using (request)
						{
							Console_WriteZMessage(request, "Receiving");

							// Frame 0: identity of client
							// Frame 1: PING, or client control frame
							// Frame 2: request body

							identity = request.Pop();

							ZFrame control = request.Pop();
							string controlMessage = control.ReadString();

							if (controlMessage == "PING")
							{
								control.Dispose();
								response.Add(new ZFrame("PONG"));
							}
							else
							{
								response.Add(control);
								response.Add(new ZFrame("OK"));
							}
						}

						response.Prepend(identity);

						Console_WriteZMessage(response, "Sending  ");
						if (!server.Send(response, out error))
						{
							if (error == ZError.ETERM)
								break;	// Interrupted
							throw new ZException(error);
						}
					}
				}
				if (error == ZError.ETERM)
				{
					Console.WriteLine("W: interrupted");
				}
			}
		}

	}
}