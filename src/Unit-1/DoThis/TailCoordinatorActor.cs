﻿using System;
using Akka.Actor;

namespace WinTail
{
	public class TailCoordinatorActor : UntypedActor
	{
		#region Message types

		/// <summary>
		/// Start tailing the file at user-specified path.
		/// </summary>
		public class StartTail
		{
			public StartTail(string filePath, ActorRef reporterActor)
			{
				FilePath = filePath;
				ReporterActor = reporterActor;
			}

			public string FilePath { get; private set; }

			public ActorRef ReporterActor { get; private set; }
		}

		/// <summary>
		/// Stop tailing the file at user-specified path.
		/// </summary>
		public class StopTail
		{
			public StopTail(string filePath)
			{
				FilePath = filePath;
			}

			public string FilePath { get; private set; }
		}


		#endregion

		protected override void OnReceive(object message)
		{
			if (message is StartTail)
			{
				var msg = message as StartTail;
				// here we are creating our first parent/child relationship!
				// the TailActor instance created here is a child
				// of this instance of TailCoordinatorActor
				Context.ActorOf(Props.Create(() => new TailActor(msg.ReporterActor, msg.FilePath)));
			}
		}

		protected override SupervisorStrategy SupervisorStrategy()
		{
			return new OneForOneStrategy(
				10, 
				TimeSpan.FromSeconds(30), 
				decider: x =>
				{
					if (x is ArithmeticException)
						return Directive.Resume;
					if (x is NotSupportedException)
						return Directive.Stop;
					return Directive.Restart;
				});
		}
	}
}

