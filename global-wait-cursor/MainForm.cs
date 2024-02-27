namespace global_wait_cursor
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            _progressDlg = new ProgressDlg(this);
            Disposed += (sender, e) =>_progressDlg.Dispose();
            buttonShow.Click += (sender, e) =>
            {
                _progressDlg.ShowDialog(this);
            };
        }
        ProgressDlg _progressDlg;
    }
}
