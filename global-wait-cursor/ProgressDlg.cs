
namespace global_wait_cursor
{
    public partial class ProgressDlg : Form
    {
        private CancellationTokenSource? cts = null;
        public ProgressDlg(Form owner)
        {
            InitializeComponent();
            Owner = owner;
            bnStart.Click += (sender, e) =>
            {
                if(cts != null)
                {
                    cts.Dispose();
                }
                cts = new CancellationTokenSource();
                _taskWork = Start(cts.Token);
            };
            bnCancel.Click += (sender, e) => cts.Cancel();
        }
        Task _taskWork;

        Color GREEN_SCREEN = Color.FromArgb(0x0C, 0xFF, 0x00);
        public async Task Start(CancellationToken ct)
        {
            using (var overlay = new Form
            {
                Owner = Owner,
                Visible = false,
                Location = Owner.Location,
                Size = Owner.Bounds.Size,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = GREEN_SCREEN,
                TransparencyKey = GREEN_SCREEN,
                Cursor = Cursors.WaitCursor,
            })
            {
                overlay.Visible = true;
                BringToFront();
                //ToDo: Find a way to show WaitCursor in all windows!
                UseWaitCursor = true;

                cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                await Task.Run(() =>
                {
                    try
                    {
                        for (int i = 1; i <= 10; i++)
                        {
                            BeginInvoke(() => progressBar.Value = 10 * i);
                            // We're already on a background thread, so simulate
                            // the work as blocking on the bacground thread.
                            Task.Delay(TimeSpan.FromSeconds(1), cts.Token).Wait();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        cts.Dispose();
                    }
                }, cts.Token);
            }
            progressBar.Value = 0;
            UseWaitCursor = false;
            Close();
            ShowDialog(Owner);
        }
    }
}
