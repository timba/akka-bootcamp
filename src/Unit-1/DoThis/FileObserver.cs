﻿using System;
using Akka.Actor;
using System.IO;

namespace WinTail
{
	/// <summary>
	/// Turns <see cref="FileSystemWatcher"/> events about a specific file into messages for <see cref="TailActor"/>.
	/// </summary>
	public class FileObserver : IDisposable
	{
		private readonly ActorRef _tailActor;
        private FileSystemWatcher _watcher;
        private readonly string _fileDir;
        private readonly string _fileNameOnly;

		public FileObserver(ActorRef tailActor, string absoluteFilePath)
		{
			_tailActor = tailActor;
			_fileDir = Path.GetDirectoryName(absoluteFilePath);
			_fileNameOnly = Path.GetFileName(absoluteFilePath);
		}

		public void Start()
		{
#if MONO
			Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "enabled");
#endif

			// make watcher to observe our specific file
			_watcher = new FileSystemWatcher(_fileDir, _fileNameOnly);

			// watch our file for changes to the file name, or new messages being written to file
			_watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

			// assign callbacks for event types
			_watcher.Changed += OnFileChanged;
			_watcher.Error += OnFileError;

            // start watching
            _watcher.EnableRaisingEvents = true;
		}

		/// <summary>
		/// Stop monitoring file.
		/// </summary>
		public void Dispose()
		{
			_watcher.Dispose();
		}

		/// <summary>
		/// Callback for <see cref="FileSystemWatcher"/> file error events.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnFileError(object sender, ErrorEventArgs e)
		{
			_tailActor.Tell(new TailActor.FileError(_fileNameOnly, e.GetException().Message), ActorRef.NoSender);
		}

		/// <summary>
		/// Callback for <see cref="FileSystemWatcher"/> file change events.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType == WatcherChangeTypes.Changed)
			{
				// here we use a special ActorRef.NoSender
				// since this event can happen many times, this is a little microoptimization
				_tailActor.Tell(new TailActor.FileWrite(e.Name), ActorRef.NoSender);
			}
		}
	}
}

