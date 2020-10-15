using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;

namespace spaceInvader
{
    delegate void GetData(string msg);
	// TODO: статические классы лучше лишний раз не делать
    /// <summary>
    /// Класс, в котором описано поведение игры (все действия игры)
    /// </summary>
    static class Game
    {
        private static BufferedGraphicsContext _context;
        /// <summary>
        /// буфер в памяти
        /// </summary>
        public static BufferedGraphics Buffer;

        /// <summary>
        /// статитческий массив объектов BaseObject (базовых объектов)
        /// </summary>
        public static BaseObject[] _objs;
        /// <summary>
        /// статитческий список объектов Bullet (пули выпускаемые кораблем)
        /// </summary>
        private static List<Bullet> _bullet;

        /// <summary>
        /// статитческий массив объектов Asteroid (астероид)
        /// </summary>
        private static List<Asteroid> _asteroids;

        /// <summary>
        /// статитческий массив объектов Medkit (аптечки)
        /// </summary>
        private static Medkit[] _medkits;

        /// <summary>
        /// статитческий обьект Корабль +
        /// </summary>
        private static Ship _ship = new Ship(new Point(10, 400), new Point(5, 5), new Size(50, 50));

        // ширина игрового поля
        public static int Width { get; set; }
        // высота игрового поля
        public static int Height { get; set; }
        //максимальный размер игрового поля по ширине/высоте
        private const int GameWindowMaxSize = 1000;

        /// <summary>
        /// Таймер для текущего статического класса
        /// </summary>
        private static Timer _timer = new Timer();

        /// <summary>
        /// генератор случайных чисел
        /// </summary>
        public static Random Rnd = new Random(); // TODO: никто не использует

        //Количество астероидов (начальное)
        private static int AsteroidsCount = 1; // TODO: приватные переменные с заглавной буквы не называют
        //Количество добовляемых астероидов в новой волне
        private static int AsteroidsCountIncrement = 1;

        static Game()
        {
            _bullet = new List<Bullet>();
            _asteroids = new List<Asteroid>(5);
        }

        private static void ValidateGameWindowSize(int width, int height)
        {
            if (width > GameWindowMaxSize || width < 0 || height > GameWindowMaxSize || height < 0)
                throw new ArgumentOutOfRangeException($"Значения ширины / высоты игрового поля должны находиться в диапазоне [0,{GameWindowMaxSize}]");
        }

        /// <summary>
        /// Инициализация сцены и обьектов
        /// </summary>
        /// <param name="form"></param>
        public static void Load()
        {
            _objs = new BaseObject[180];
            _medkits = new Medkit[2];
            var rnd = new Random();
            for (var i = 0; i < _objs.Length; i++)
            {
                int r = rnd.Next(5, 50);
                _objs[i] = new Star(new Point(rnd.Next(0, Game.Width), rnd.Next(1, Game.Height)), new Point(-r / 10, r), new Size(3, 3));
            }

            for (var i = 0; i < _medkits.Length; i++)
            {
                int r = rnd.Next(5, 50);
                _medkits[i] = new Medkit(new Point(Game.Width, rnd.Next(1, Game.Height - 25)), new Point(-r / 5, r), new Size(20, 20));
            }
            GenerateAsteroid(AsteroidsCount);
        }

        public static void GenerateAsteroid(int Count)
        {
            _asteroids.Clear();
            var rnd = new Random();
            for (var i = 0; i < Count; i++)
            {
                int r = rnd.Next(20, 50);
				// TODO: Game.Width, Game.Height - убрать Game или заменить его на this
                _asteroids.Add(new Asteroid(new Point(Game.Width, rnd.Next(1, Game.Height - 25)), new Point(-r / 5, r), new Size(r, r)));
            }
        }
        /// <summary>
        /// Инициализация сцены и обьектов
        /// </summary>
        /// <param name="form"></param>
        public static void Init(Form form)
        {
            // Графическое устройство для вывода графики
            Graphics g;
            // Предоставляет доступ к главному буферу графического контекста для текущего приложения

            form.KeyDown += Form_KeyDown;

            _context = BufferedGraphicsManager.Current;
            g = form.CreateGraphics();

            // Создаем объект (поверхность рисования) и связываем его с формой
            // Запоминаем размеры формы

            Width = form.ClientSize.Width;
            Height = form.ClientSize.Height;
            ValidateGameWindowSize(Width, Height);
            // Связываем буфер в памяти с графическим объектом, чтобы рисовать в буфере

            Buffer = _context.Allocate(g, new Rectangle(0, 0, Width, Height));
            _timer.Interval = 45;
            _timer.Start();
            _timer.Tick += Timer_Tick;

            Load();
            Ship.MessageDie += Finish;
            Bullet.MessageBulletDestroyed += Bullet.ShowMessageBulletDestroyed;
            Bullet.MessageBulletCreated += Bullet.ShowMessageBulletCreated;
            Ship.LooseEnergy += Ship.ShowMessageShipLooseEnergy;
            Ship.AddEnergy += Ship.ShowMessageShipAddEnergy;
            Ship.AddScore += Ship.ShowMessageShipAddScore;
        }

        /// <summary>
        /// Обработчик таймера в котором вызываются Draw () и Update();
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Timer_Tick(object sender, EventArgs e)
        {
            Draw();
            Update();
        }

        /// <summary>
        /// вывод графики
        /// </summary>
        public static void Draw()
        {
            Buffer.Graphics.Clear(Color.Black);
            foreach (BaseObject obj in _objs)
                obj.Draw();
            foreach (Asteroid a in _asteroids)
                a?.Draw(); // TODO: зачем проверки на null?
            foreach (Medkit m in _medkits)
                m?.Draw();
            foreach (Bullet bul in _bullet)
                bul?.Draw();
            _ship.Draw();
            if (_ship != null)
            {
                Buffer.Graphics.DrawString("Energy:" + _ship.Energy, SystemFonts.DefaultFont, Brushes.Green, 0, 0);
                Buffer.Graphics.DrawString("Score:" + _ship.Score, SystemFonts.DefaultFont, Brushes.Yellow, 100, 0);
            }
            Buffer.Render();

        }

        /// <summary>
        /// изменения состояния объектов
        /// </summary>
        public static void Update()
        {
            foreach (BaseObject obj in _objs) obj.Update();
            foreach (Bullet bul in _bullet) bul.Update();

            for (var i = 0; i < _asteroids.Count; i++)
            {
                if (AsteroidsCount == 0)
                {
                    GenerateAsteroid(_asteroids.Count + AsteroidsCountIncrement);
                    AsteroidsCount = _asteroids.Count;
                }
                if (_asteroids[i] == null) continue;
                _asteroids[i].Update();
                for (var j = 0; j < _bullet.Count; j++)
                {
                    if (_asteroids[i] != null && _bullet[j].Collision(_asteroids[i]))
                    {
                        System.Media.SystemSounds.Hand.Play();
                        _asteroids[i] = null;
                        _bullet.RemoveAt(j);
                        _ship?.IncreaseScore(50);
                        j--;
                        AsteroidsCount--;
                    }
                }
                if (_asteroids[i] == null || !_ship.Collision(_asteroids[i])) continue;
                {
                    var rnd = new Random();
                    _ship?.EnergyLow(rnd.Next(10, 20));
                    System.Media.SystemSounds.Asterisk.Play();
                    _asteroids[i] = null;
                    AsteroidsCount--;
                }
				// TODO: зачем проверка на null, если в условии ее нет и ship создали при объявлении?
				if (_ship.Energy <= 0) _ship?.Die();  
			}

            for (var i = 0; i < _medkits.Length; i++)
            {
                if (_medkits[i] == null) continue;
                _medkits[i].Update();
                for (var j = 0; j < _bullet.Count; j++)
                {
                    if (_medkits[i] != null && _bullet[j].Collision(_medkits[i]))
                    {
                        System.Media.SystemSounds.Hand.Play();
                        _medkits[i] = null;
                        _bullet.RemoveAt(j);
                        _ship?.IncreaseEnergy(20);
                        j--;
                    }
                }
                if (_medkits[i] == null || !_ship.Collision(_medkits[i])) continue;
                {
                    _ship?.IncreaseEnergy(20);
                    _medkits[i] = null;
                    System.Media.SystemSounds.Asterisk.Play();
                }
            }
        }

        /// <summary>
        /// Метод завершение игры
        /// </summary>
        public static void Finish()
        {
            _timer.Stop();
            WriteGameMessage("Игра окончена!"); // TODO: недостаточно пафоса
        }

        /// <summary>
        /// обработка событий нажатия Ctrl  выстрел, Up -сдвиг корабля вверх, Down -сдвиг корабля вниз
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                _bullet.Add(new Bullet(new Point(_ship.Rect.X + 50, _ship.Rect.Y + 25), new Point(4, 0), new Size(15, 1)));
                _bullet[_bullet.Count - 1].MessageCreate();
            }
            if (e.KeyCode == Keys.Up) _ship.Up();
            if (e.KeyCode == Keys.Down) _ship.Down();
        }

        /// <summary>
        /// внутреигровое сообщение (с выравниванием по центру)
        /// </summary>
        /// <param name="message"></param>
        private static void WriteGameMessage(string message)
        {
            SizeF MessageSize = Buffer.Graphics.MeasureString(message, new Font(FontFamily.GenericSansSerif, 60, FontStyle.Underline));
            Buffer.Graphics.DrawString(message, new Font(FontFamily.GenericSansSerif, 60, FontStyle.Underline), Brushes.White, (Width - MessageSize.Width) / 2, Height / 2);
            Buffer.Render();
        }
    }
}