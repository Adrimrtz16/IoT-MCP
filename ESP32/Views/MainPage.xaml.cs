using Microsoft.Maui.Controls.Shapes;
using ESP32.ViewModels;

namespace ESP32
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = new MainPageViewModel();
            BindingContext = _viewModel;

            // Suscribirse al evento
            _viewModel.OnMessageReceived += AddMessageToChat;
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            _viewModel.OnSendClicked(MessageEntry.Text, text => MessageEntry.Text = text);
        }

        private void AddMessageToChat(string text, bool isUser)
        {
            var frame = new Border
            {
                BackgroundColor = isUser ? Color.FromArgb("#007AFF") : Color.FromArgb("#E5E5EA"),
                Padding = new Thickness(12, 10),
                HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
                MaximumWidthRequest = 280,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Content = new Label
                {
                    Text = text,
                    TextColor = isUser ? Colors.White : Colors.Black,
                    FontSize = 15
                }
            };
            MessagesContainer.Children.Add(frame);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50);
                await ChatScrollView.ScrollToAsync(MessagesContainer, ScrollToPosition.End, true);
            });
        }
    }
}