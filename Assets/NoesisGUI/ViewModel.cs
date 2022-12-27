#if UNITY_5_3_OR_NEWER
#define NOESIS
using Noesis;
using System.ComponentModel;
using System.Windows.Input;
using Unity.VisualScripting;
using UnityEngine;
#else
using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Input;
#endif

namespace TotalMinerUnity.Menus
{
    public enum State
    {
        Main,
        Start,
        Settings
    }

    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        public ICommand Start { get; private set; }
        public ICommand Settings { get; private set; }
        public ICommand Exit { get; private set; }
        public ICommand Back { get; private set; }

        public ViewModel()
        {
            Start = new DelegateCommand(OnStart);
            Settings = new DelegateCommand(OnSettings);
            Exit = new DelegateCommand(OnExit);
            Back = new DelegateCommand(OnBack);

            State = State.Main;

        }

        public string Platform { get { return "PC"; } }

        private State _state;
        public State State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged("State");
                }
            }
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnStart(object parameter)
        {
            State = State.Start;
#if NOESIS
            World w = (new GameObject("World")).AddComponent<World>();
            w.Init();
            //need loading screen

#endif
        }

        private void OnSettings(object parameter)
        {
            State = State.Settings;
        }

        private void OnExit(object parameter)
        {
#if NOESIS
            Debug.Log("Exiting game");
            Application.Quit();
#endif
        }

        private void OnBack(object parameter)
        {
            switch (State)
            {
                case State.Main:
                    break;
                case State.Start:
                case State.Settings:
                    {
                        State = State.Main;
                        break;
                    }
            }
        }

    }
}