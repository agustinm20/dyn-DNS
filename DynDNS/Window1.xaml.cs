using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Net;
using Utility.ModifyRegistry;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Delay;


namespace DynDNS
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
    
      public Window1()
        {

            try
            {
            
           
            InitializeComponent();

            //Tray Icon
            MinimizeToTray.Enable(this);
         
            //Iniciar con windows
            ModifyRegistry reg = new ModifyRegistry();
            reg.BaseRegistryKey = Registry.LocalMachine;
          
            reg.SubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

            string val=reg.Read("DYNDNS");

            if (val == null)
            {
                string path = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\DynDNS.exe"; ;
                
                reg.Write("DYNDNS", path.ToString());
                
                              
            }
                         

            //Cargar el login
            Usuario u = LeerArchivo();
            if (u != null)
            {
                this.txt_user.Text = u.User;
                this.txt_pass.Text = u.Pass;
                this.lbl_IP_ant.Content = u.IP.Trim();
            }

            //Obtengo la IP pública
            WebClient client = new WebClient();
                       
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR1.0.3705;)");

            string baseurl = "http://checkip.dyndns.org/";

            Stream data = client.OpenRead(baseurl);
            StreamReader reader = new StreamReader(data);
            string s = reader.ReadToEnd();
            data.Close();
            reader.Close();
            s = s.Replace("<html><head><title>Current IP Check</title></head><body>", "").Replace("</body></html>", "").ToString();
            s = s.Replace("Current IP Address:", "");
            lbl_IP_new.Content = s;
      }catch (Exception e)
            {
                System.Windows.MessageBox.Show("Error al conectarse a internet. " + e.Message);
            }    
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
              try
            {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 1, 0);
            dispatcherTimer.Start();
            lbl_time.Content = "";
  
            }
              catch (Exception ex)
              {
                  System.Windows.MessageBox.Show("Error cargar la ventana. " + ex.Message);
              }     
            
        }


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
              try
            {
            Usuario u = LeerArchivo();
            string user;
            string pass_orig;
            string IP;
            string pass;
            if (u == null)
            {
                 user = txt_user.Text.Trim();
                 pass_orig = txt_pass.Text.Trim();
                 IP = "";
                 pass = toMD5(pass_orig);
            }
            else
            {
                user = u.User;
                pass_orig = u.Pass;
                IP = u.IP;
                pass = toMD5(pass_orig);
            }
           

            String baseUri = "https://dinamico.cdmon.org/onlineService.php?enctype=MD5&n=" + user.ToString() + "&p=" + pass.ToString() + "";
            HttpWebRequest connection =
            (HttpWebRequest)HttpWebRequest.Create(baseUri);

            connection.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)connection.GetResponse();

            StreamReader sr =
            new StreamReader(response.GetResponseStream(),Encoding.UTF8);

            // Leer el contenido.
            string res = sr.ReadToEnd();

            string[] Resultado = res.Split('&');
            foreach (string Parte in Resultado)
            {
                if (Parte != "")
                {
                    string[] ResultadoDoble = Parte.Split('=');
                    bool SeEjecutoBien = true;
                    if (ResultadoDoble[0] == "resultat")
                    {
                        if (ResultadoDoble[1] == "errorlogin")
                        {
                            lbl_info.Content = ("Ocurrió un error con el \nnombre de usuario contraseña.\nPor favor verifique.");

                            SeEjecutoBien = false;
                        }
                        else if (ResultadoDoble[1] == "badip")
                        {
                            lbl_info.Content = ("La Autentificación fué realizada\ncon éxito; pero la ip fué\nreconocida como inválida!");

                            SeEjecutoBien = false;
                        }
                        else if (ResultadoDoble[1] == "guardatok")
                        {
                            lbl_time.Content = "Últ. Act.: " + DateTime.Now.ToString();

                            SeEjecutoBien = true;
                        }
                    }
                    else if (ResultadoDoble[0] == "newip")
                    {
                        if (SeEjecutoBien == true)
                        {
                            

                            lbl_info.Content = "Su dominio esta apuntando a: " + ResultadoDoble[1];
                            string ip1=IP;
                            string ip2= ResultadoDoble[1];
                            if (ip1 != ip2) //Cambió la IP del servidor local
                            {
                                lbl_IP_ant.Content = ip1;
                                lbl_IP_new.Content = ip2;
                                Usuario us = new Usuario();
                                us.User = user;
                                us.Pass = pass_orig;
                                us.IP = ResultadoDoble[1].Trim().ToString();
                                GuardarArchivo(us);
                                string asunto=" IP anterior: " + ip1.ToString() + " // IP Nueva: " + ip2.ToString();
                                SendMail(user,asunto);
                            }
                          
                            //lbl_IP_new.Content = ResultadoDoble[1].Trim().ToString();
                        }
                    }
                }
            }

            // Cerrar los streams abiertos.
            sr.Close();
            response.Close();
            }
              catch (Exception ex)
              {
                  lbl_info.Content="Error al conectar al servidor de DNS. " + ex.Message;
              }
        }


        private string richTextBox2String(System.Windows.Controls.RichTextBox rtb)
        {

            TextRange textRange = new TextRange(rtb.Document.ContentStart,

                rtb.Document.ContentEnd);

            return textRange.Text;

        }


        private void btn_Actualizar_Click(object sender, RoutedEventArgs e)
        {

          try
          {
              Usuario u = LeerArchivo();
              string user;
              string pass_orig;
              string IP;
              string pass;
              if (u == null)
              {
                  user = txt_user.Text.Trim();
                  pass_orig = txt_pass.Text.Trim();
                  IP = "";
                  pass = toMD5(pass_orig);
              }
              else
              {
                  user = u.User;
                  pass_orig = u.Pass;
                  IP = u.IP;
                  pass = toMD5(pass_orig);
              }

              String baseUri = "https://dinamico.cdmon.org/onlineService.php?enctype=MD5&n=" + user.ToString() + "&p=" + pass.ToString() + "";
            HttpWebRequest connection =
            (HttpWebRequest)HttpWebRequest.Create(baseUri);

            connection.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)connection.GetResponse();

            StreamReader sr =
            new StreamReader(response.GetResponseStream(),
            Encoding.UTF8);

            // Leer el contenido.
            string res = sr.ReadToEnd();

           string[] Resultado = res.Split('&');
			foreach(string Parte in Resultado)
			{
                if (Parte != "")
                {
                    string[] ResultadoDoble = Parte.Split('=');
                    bool SeEjecutoBien = true;
                    if (ResultadoDoble[0] == "resultat")
                    {
                        if (ResultadoDoble[1] == "errorlogin")
                        {
                            System.Windows.MessageBox.Show("Ocurrió un error con el \nnombre de usuario contraseña.\nPor favor verifique.");

                            SeEjecutoBien = false;
                        }
                        else if (ResultadoDoble[1] == "badip")
                        {
                            System.Windows.MessageBox.Show("La Autentificación fué realizada\ncon éxito; pero la ip fué\nreconocida como inválida!");

                            SeEjecutoBien = false;
                        }
                        else if (ResultadoDoble[1] == "guardatok")
                        {
                            lbl_time.Content = "Últ. Act.: " + DateTime.Now.ToString();
                            SeEjecutoBien = true;
                        }
                    }
                    else if (ResultadoDoble[0] == "newip")
                    {
                        if (SeEjecutoBien == true)
                        {
                            System.Windows.MessageBox.Show("Actualización realizada con éxito!\nNueva ip: " + ResultadoDoble[1]);

                            lbl_info.Content = "Su dominio esta apuntando a: " + ResultadoDoble[1];
                            string ip1 = IP;
                            string ip2 = ResultadoDoble[1];
                            if (ip1 != ip2) //Cambió la IP del servidor local
                            {
                                lbl_IP_ant.Content = ip1;
                                lbl_IP_new.Content = ip2;
                                Usuario us = new Usuario();
                                us.User = user;
                                us.Pass = pass_orig;
                                us.IP = ResultadoDoble[1].Trim().ToString();
                                GuardarArchivo(us);
                                
                                string asunto = " IP anterior: " + ip1.ToString() + " // IP Nueva: " + ip2.ToString();
                                SendMail(user, asunto);
                            }
                        }
                    }
                }
				}

            // Cerrar los streams abiertos.
            sr.Close();
            response.Close();
              }
            catch (Exception ex)
            {
                lbl_info.Content="Error al conectar al servidor de DNS. " + ex.Message;
            }
          
        }


        public void SendMail(string user,string asunto)
        {
            try
            {
              
	      
                string mailUser = "from@mail.com.ar";
                string mailPassword = "password-from";               
                string smtpServer = "mail.mail.com.ar";
                string smtpPort = "25";
                string body = "Hola, se ha cambiado la IP dinámica del usuario " + user +
                              "<br><br>" + asunto + "<br><br>Saludos.";

                MailMessage mail = new MailMessage()
                {
                    From = new MailAddress(mailUser, "Dyn-DNS"),
                    Body = body,
                    Subject = "Actualización de IP - Dyn-DNS",
                    IsBodyHtml = true,
                };

                mail.To.Add(new MailAddress("to@mail.com.ar", "Nombre"));
               
                SmtpClient smtp = new SmtpClient();
                smtp.Host = smtpServer;
                smtp.Port = Convert.ToInt32(smtpPort);
                smtp.Credentials = new NetworkCredential(mailUser, mailPassword);
                smtp.Send(mail);



            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Error al mandar un mail. " + e.Message);
            }
        }


        public string toMD5(string Value)
        {
            //Declarations
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.ASCII.GetBytes(Value);
            data = x.ComputeHash(data);
            string ret = "";
            for (int i = 0; i < data.Length; i++)
                ret += data[i].ToString("x2").ToLower();
            return ret;
        }


        public void GuardarArchivo(Usuario u)
        {
           try{
               string sPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\_log.txt";
                System.IO.StreamWriter sw = new System.IO.StreamWriter(sPath);
                sw.WriteLine(u.User);
                sw.WriteLine(u.Pass);
                sw.WriteLine(u.IP);
                sw.Close();
           } catch (IOException ex)
           {
               System.Windows.MessageBox.Show(ex.Message);
           }
       
        }


        public Usuario LeerArchivo()
        {
            Usuario u = null;
             try{
                 string sPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\_log.txt";
               if (File.Exists(sPath))
               {
                    u = new Usuario();     
                    System.IO.StreamReader sr = new System.IO.StreamReader(sPath);
                    u.User=sr.ReadLine();
                    u.Pass=sr.ReadLine();
                    u.IP=sr.ReadLine();
                   
                    sr.Close();
               }
                return u;
           } catch (IOException ex)
           {
               System.Windows.MessageBox.Show(ex.Message);
                 return u;
           }
           
         }


        public static IPAddress GetExternalIp()
        {
            string whatIsMyIp = "http://whatismyip.com";
            string getIpRegex = @"(?<=<TITLE>.*)\d*\.\d*\.\d*\.\d*(?=</TITLE>)";
            WebClient wc = new WebClient();
            UTF8Encoding utf8 = new UTF8Encoding();
            string requestHtml = "";
            try
            {
                requestHtml = utf8.GetString(wc.DownloadData(whatIsMyIp));
            }
            catch (WebException we)
            {
                // do something with exception
                Console.Write(we.ToString());
            }
            Regex r = new Regex(getIpRegex);
            Match m = r.Match(requestHtml);
            IPAddress externalIp = null;
            if (m.Success)
            {
                externalIp = IPAddress.Parse(m.Value);
            }
            return externalIp;
        }



        private void Window_StateChanged(object sender, EventArgs e)
        {

          
        }


        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
           
        }


        private void onClose(object sender, EventArgs e)
        {
           
        }


    }
}
