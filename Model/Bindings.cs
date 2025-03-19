using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI_Telebot.Model
{
    public class Bindings : INotifyPropertyChanged
    {
        //public String BotState { get; set; } = "Бот остановлен";

        private string _BotState = "Бот остановлен";
        private string _ButtonName = "Запустить Телеграм-бот";

        public string BotState
        {
            get => _BotState;
            set
            {
                _BotState = value;
                OnPropertyChanged(nameof(BotState));
            }
        }
        public string ButtonName
        {
            get => _ButtonName;
            set
            {
                _ButtonName = value;
                OnPropertyChanged(nameof(ButtonName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
