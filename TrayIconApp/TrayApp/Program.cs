using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrayApp.Properties;

namespace TrayApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application. <para/>
        /// Sample Icon Credit: https://github.com/iconic/open-iconic
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Standard WinForm config
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create Tray icon and start program
            using (new TrayIcon())
                Application.Run();
        }

        #region Private Classes
        /// <summary>
        /// Wrapper class for a <see cref="NotifyIcon"/> which defines specific functionality
        /// </summary>
        private class TrayIcon : IDisposable
        {
            public readonly NotifyIcon Icon;
            public readonly TrayWorker Worker;
            public TrayIcon()
            {
                // Sample Definition of a Task for the Worker to Perform
                Worker = new TrayWorker(
                    delegate { MessageBox.Show($"Hi! I am a {nameof(TrayWorker)} called {nameof(Worker)}!","Example Worker"); });

                #region Create Context Menu
                var menu = new ContextMenuStrip();
                menu.Items.AddRange(new ToolStripItem[]
                {
                    #region Sample Items
                    new ToolStripMenuItem(
                        "Re-Run Worker",
                        image: Resources.loop_circular_2x,
                        onClick: delegate
                    {
                        if(!Worker.IsBusy)
                            Worker.RunWorkerAsync();
                    }),

                    new ToolStripSeparator(),

                    // Recommended Exit Procedure for Graceful Close
                    new ToolStripMenuItem(
                        "Exit",
                        image: Resources.account_logout_2x,
                        onClick: delegate
                    {
                        if(Worker.IsBusy)
                        {
                            Worker.Finished += delegate { Application.Exit(); };
                            Worker.CancelRequested = true;
                        }
                        else Application.Exit();
                    })
                    #endregion

                });
                #endregion

                #region Create NotifyIcon
                Icon = new NotifyIcon
                {
                    Icon = System.Drawing.Icon.FromHandle(Resources.bolt_2x.GetHicon()),
                    ContextMenuStrip = menu,
                    Visible = true
                };
                #endregion

                Worker.RunWorkerAsync();
            }

            public void Dispose() => Icon.Dispose();
        }

        /// <summary>
        /// Background worker who manages the functionality of a given <see cref="Action"/>
        /// </summary>
        private class TrayWorker : BackgroundWorker
        {
            #region Public Enums
            /// <summary>
            /// Enum defining modes of operation for the worker process
            /// </summary>
            public enum TrayWorkerMode
            {
                Continuous,
                Once
            }
            #endregion

            #region Events
            public EventHandler<EventArgs> Finished;
            #endregion

            #region Variable Declarations
            /// <summary>
            /// Settable value to cancel worker after current iteration
            /// </summary>
            public bool? CancelRequested;
            #endregion
            
            /// <summary>
            /// Logic to perform an action once or continuously while Tray app is alive
            /// </summary>
            /// <param name="mode">Whether the given logic runs once or many times</param>
            public TrayWorker(Action action, TrayWorkerMode mode = TrayWorkerMode.Once) : base()
            {

                DoWork += async delegate
                {
                    if (Thread.CurrentThread.Name == default(string))
                        Thread.CurrentThread.Name = $"TrayWorker Thread ({new Guid()})";

                    bool _cancelRequested = false;
                    while (!_cancelRequested)
                    {
                        var start = DateTime.Now;

                        lock (this)
                            _cancelRequested = CancelRequested ?? false;
                        if (mode == TrayWorkerMode.Once) _cancelRequested = true;

                        action.Invoke();

                        if (!_cancelRequested)
                        {
                            #region Compute Timespan and Wait
                            var timespan = DateTime.Now - start;
                            var waitTime = Math.Max(Settings.Default.CycleTimeMillis - timespan.Milliseconds, 0);
                            await Task.Delay(waitTime);
                            #endregion
                        }
                    }

                    Finished?.Invoke(this, new EventArgs());
                };
            }
            
        }
        #endregion
    }

}
