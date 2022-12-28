#if UNITY_5_3_OR_NEWER
#define NOESIS
using Noesis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Profiling.HierarchyFrameDataView;
#else
using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
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


        public ICommand ChangeBackground1 { get; private set; }
        public ICommand ChangeBackground2 { get; private set; }
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
            ChangeBackground1 = new DelegateCommand(OnChangeBackground1);
            ChangeBackground2 = new DelegateCommand(OnChangeBackground2);

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

        int _bs = 0;
        public int bs
        {
            get { return _bs; }
            set
            {
                if (_bs != value)
                {
                    _bs = value;
                    OnPropertyChanged("bs");
                }
            }
        }

#if NOESIS
        private Queue<Texture2D> MainMenuBackgroundInUse = new Queue<Texture2D>();
#endif
        public ImageSource SomeImage
        {
            get
            {
#if NOESIS
                Texture2D texture = Globals.MainMenuBackgrounds.Dequeue();
                MainMenuBackgroundInUse.Enqueue(texture);

                if(MainMenuBackgroundInUse.Count > 2)
                    Resources.UnloadAsset(MainMenuBackgroundInUse.Dequeue());

                return new TextureSource(texture);

#else
                return default;
#endif
            }
        }


        public void test()
        {
#if NOESIS
            Debug.Log("hit2");
#endif
        }

        public ImageSource SomeImage2
        {
            get 
            {
#if NOESIS
                Texture2D texture = Globals.MainMenuBackgrounds.Dequeue();
                MainMenuBackgroundInUse.Enqueue(texture);

                if (MainMenuBackgroundInUse.Count > 2)
                    Resources.UnloadAsset(MainMenuBackgroundInUse.Dequeue());

                return new TextureSource(texture);

#else
                return default;
#endif
            }
        }

        public void OnPropertyChanged(string name)
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

        private void OnChangeBackground1(object parameter)
        {
#if NOESIS

            OnPropertyChanged("SomeImage");
#endif
        }

        bool background1Active = true;
        private void OnChangeBackground2(object parameter)
        {
#if NOESIS
            OnPropertyChanged("SomeImage2");
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