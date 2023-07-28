using System.Windows;

namespace NecroLink
{
    public partial class ResultsWindow : Window
    {
        public ResultsWindow()
        {
            InitializeComponent();
        }

        public void ShowMessage(string message)
        {
            // Assuming you have a TextBlock or similar control in your ResultsWindow to display the message
            this.MessageTextBlock.Text = message;
        }
    }
}