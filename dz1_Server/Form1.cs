using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace dz1_Server
{
    public partial class Form1 : Form
    {
        public SynchronizationContext uiContext;
        public Form1()
        {
            InitializeComponent();
            uiContext = SynchronizationContext.Current;
        }

        // метод для получения сообщений от клиента
        public void ForRecieveMessage(object param)
        {
            Socket handler = (Socket)param;
            try
            {
                // создаем буфер
                byte[] bytes = new byte[1024];
                // получение сообщения, Receive возвращает фактически считанное число байтов
                int bytesRec = handler.Receive(bytes);
                //MessageBox.Show(bytesRec.ToString());
                // создаем строку и записываем туда конвертированное сообщение
                string client = Encoding.Default.GetString(bytes, 0, bytesRec);
                // достаем информацию об ip-адресе и порте клиента с помощью RemoteEndPoint
                client += "(" + handler.RemoteEndPoint.ToString() + ")";
                string message = null;  // строка для ответа клиенту
                while (true)
                {
                    // принимаем данные, переданные клиентом
                    bytesRec = handler.Receive(bytes);
                    //MessageBox.Show(bytesRec.ToString());
                    if (bytesRec == 0)  // если данных нет
                    {
                        // поток блокируем - закрываем сокет
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        return;
                    }
                    string data = Encoding.Default.GetString(bytes, 0, bytesRec);
                    
                    // переменная для текущего времени
                    DateTime dt = DateTime.Now;
                    if (data.IndexOf("Hi") > -1)  // если клиент отправил Hi
                    {
                        // вызываем метод для ответа, где прописаны варианты ответа на 
                        // Hi в зависимости от времени суток
                        message = AnswerOnHi(dt, message);
                    }
                    else if(data.IndexOf("<end>") > -1)  // если клиент отправил <end>
                    {
                        break;  // выход из цикла
                    }
                    else  // если клиент отправил что-то другое
                    {
                        message = "Я такого не знаю";  // универсальный ответ
                    }
                    // сообщение конвертируем в массив ьайт
                    byte[] m = Encoding.Default.GetBytes(message);
                    // отправляем сообщение клиенту
                    handler.Send(m);
                    // в листБлкс сервера записываем инфо о клиенте и данные, что ввел клиент
                    uiContext.Send(d => listBox1.Items.Add(client), null);
                    uiContext.Send(d => listBox1.Items.Add(data), null);

                }
                string theReply = "Я завершаю обработку сообщений";
                byte[] msg = Encoding.Default.GetBytes(theReply); // конвертируем строку в массив байтов
                handler.Send(msg); // отправляем клиенту сообщение
                handler.Shutdown(SocketShutdown.Both); // Блокируем передачу и получение данных для объекта Socket.
                handler.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Сервер: " + ex.Message);
                handler.Shutdown(SocketShutdown.Both); // Блокируем передачу и получение данных для объекта Socket.
                handler.Close(); // закрываем сокет
            }
        }

        // метод для подключения клиента
        public void ForAcceptClient()
        {
            try
            {
                // конечная точка - любой IP-адрес + конкретный порт сервера
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 49153);
                // создаем сокет
                Socket sock_listener = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);
                // связываем сокет с локальной конечной точкой
                sock_listener.Bind(endPoint);
                // устанавливаем сокет в состояние прослушки
                sock_listener.Listen(10);
                MessageBox.Show($"Сервер готов слушать.");

                while(true)
                {
                    // метод Accept() принимает клиент, который хочет присоединится
                    Socket sock_handler = sock_listener.Accept();
                    // в новом потоке вызываем метод Получить сообщение от клиента
                    Thread th = new Thread(new ParameterizedThreadStart(ForRecieveMessage));
                    th.IsBackground = true;
                    th.Start(sock_handler);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Сервер: " + ex.Message);
            }
        }

        // обработка кнопки Создать сокет и слушать
        private void button1_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(ForAcceptClient));
            thread.IsBackground = true;
            thread.Start();
        }

        // метод - варианты ответа на Hi в зависимости от времени суток
        public string AnswerOnHi(DateTime dt, string message)
        {
            if (dt.TimeOfDay.Hours <= 9)
                message = "Good morning";
            else if (dt.TimeOfDay.Hours > 9 && dt.TimeOfDay.Hours < 18)
                message = "Good day";
            else
                message = "Good evening";
            return message;
        }
    }
}
