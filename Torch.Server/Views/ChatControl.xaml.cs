﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Torch;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using Torch.API;
using Torch.Managers;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    public partial class ChatControl : UserControl
    {
        private TorchBase _server;
        private MultiplayerManager _multiplayer;

        public ChatControl()
        {
            InitializeComponent();
        }

        public void BindServer(ITorchServer server)
        {
            _server = (TorchBase)server;
            _multiplayer = (MultiplayerManager)server.Multiplayer;
            DataContext = _multiplayer;

            ChatItems.Inlines.Clear();
            _multiplayer.ChatHistory.ForEach(InsertMessage);
            if (_multiplayer.ChatHistory is INotifyCollectionChanged ncc)
                ncc.CollectionChanged += ChatHistory_CollectionChanged;
            ChatScroller.ScrollToBottom();
        }

        private void ChatHistory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (IChatMessage msg in e.NewItems)
                InsertMessage(msg);
        }

        private void InsertMessage(IChatMessage msg)
        {
            bool atBottom = ChatScroller.VerticalOffset + 8 > ChatScroller.ScrollableHeight;
            var span = new Span();
            span.Inlines.Add($"{msg.Timestamp} ");
            span.Inlines.Add(new Run(msg.Name) { Foreground = msg.Name == "Server" ? Brushes.DarkBlue : Brushes.Blue });
            span.Inlines.Add($": {msg.Message}");
            span.Inlines.Add(new LineBreak());
            ChatItems.Inlines.Add(span);
            if (atBottom)
                ChatScroller.ScrollToBottom();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            OnMessageEntered();
        }

        private void Message_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OnMessageEntered();
        }

        private void OnMessageEntered()
        {
            //Can't use Message.Text directly because of object ownership in WPF.
            var text = Message.Text;
            if (string.IsNullOrEmpty(text))
                return;

            var commands = _server.Commands;
            if (commands.IsCommand(text))
            {
                _multiplayer.ChatHistory.Add(new ChatMessage(DateTime.Now, 0, "Server", text));
                _server.Invoke(() =>
                {
                    var response = commands.HandleCommandFromServer(text);
                    Dispatcher.BeginInvoke(() => OnMessageEntered_Callback(response));
                });
            }
            else
            {
                _server.Multiplayer.SendMessage(text);
            }
            Message.Text = "";
        }

        private void OnMessageEntered_Callback(string response)
        {
            if (!string.IsNullOrEmpty(response))
                _multiplayer.ChatHistory.Add(new ChatMessage(DateTime.Now, 0, "Server", response));
        }
    }
}
