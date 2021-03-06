﻿using DebuggerEngine;
using DebuggerEngine.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using WinDbgX.Models;
using WinDbgX.UICore;

#pragma warning disable 649

namespace WinDbgX.ViewModels {
	[TabItem("Processes & Threads", Icon = "/icons/gears.ico")]
	[Export]
	class ThreadsViewModel : TabItemViewModelBase, IPartImportsSatisfiedNotification {
		Dispatcher _dispatcher;
		ObservableCollection<ProcessViewModel> _processes = new ObservableCollection<ProcessViewModel>();

		public IList<ProcessViewModel> Processes => _processes;

		[Import]
		DebugManager DebugManager;

		public ThreadsViewModel() {
			_dispatcher = Dispatcher.CurrentDispatcher;
		}

		private void Debugger_ProcessExited(object sender, ProcessExitedEventArgs e) {
			_dispatcher.InvokeAsync(() => {
				_processes.Remove(_processes.First(p => p.ProcessId == e.Process.PID));
			});
		}

		private void Debugger_ThreadExited(object sender, ThreadExitedEventArgs e) {
			_dispatcher.InvokeAsync(() => {
				var threads = _processes.First(p => p.ProcessId == e.Process.PID).Threads;
				threads.Remove(threads.First(th => th.OSID == e.Thread.TID));
			});
		}

		private void Debugger_ThreadCreated(object sender, ThreadCreatedEventArgs e) {
			_dispatcher.InvokeAsync(() => {
				_processes[(int)e.Thread.ProcessIndex].Threads.Add(new ThreadViewModel(e.Thread));
			});
		}

		private void Debugger_ProcessCreated(object sender, ProcessCreatedEventArgs e) {
			_dispatcher.InvokeAsync(() => {
				_processes.Add(new ProcessViewModel(e.Process));
			});
		}

		private void Debugger_StatusChanged(object sender, StatusChangedEventArgs e) {
			var status = e.NewStatus;
			_dispatcher.InvokeAsync(() => {
				Status = status;
				if (Status == DEBUG_STATUS.NO_DEBUGGEE) {
					_dispatcher.InvokeAsync(() => Processes.Clear());
				}
			});
		}

		public void OnImportsSatisfied() {
			foreach (var process in DebugManager.Processes) {
				var processVM = new ProcessViewModel(process);
				foreach (var th in process.Threads)
					processVM.Threads.Add(new ThreadViewModel(th));
				_processes.Add(processVM);
			}

			Status = DebugManager.Status;
			DebugManager.Debugger.StatusChanged += Debugger_StatusChanged;

			DebugManager.Debugger.ProcessCreated += Debugger_ProcessCreated;
			DebugManager.Debugger.ThreadCreated += Debugger_ThreadCreated;
			DebugManager.Debugger.ThreadExited += Debugger_ThreadExited;
			DebugManager.Debugger.ProcessExited += Debugger_ProcessExited;
		}

		private DEBUG_STATUS _status;

		public DEBUG_STATUS Status {
			get { return _status; }
			private set {
				if (SetProperty(ref _status, value)) {
					OnPropertyChanged(nameof(IsEnabled));
				}
			}
		}

		public bool IsEnabled => Status == DEBUG_STATUS.BREAK;

	}
}
