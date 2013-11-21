using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using Liquid;
using System.Windows.Interop;
using System.Runtime.InteropServices.Automation;
using System.Net.NetworkInformation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Browser;

namespace Share_PointSIOC
{
    public partial class MainPage : UserControl
    {
        //private const string IMAGEN_URL = @"\\images\down.png";
        //---- Permisos para que la Aplicación tenga acceso a la Ruta ----//
        public MainPage()
        {
            DispatcherTimer Dtimer = new DispatcherTimer();
            Dtimer.Interval = new TimeSpan(0, 0, 1);
            Dtimer.Tick += new EventHandler(Dtimer_Tick);
            Dtimer.Start();
            InitializeComponent();
            Application.Current.InstallStateChanged += new EventHandler(Current_InstallStateChanged);
            ActualizarInterfaz();

            if (App.Current.HasElevatedPermissions)
            {
                oob.Visibility = Visibility.Visible;
                fileTree.Visibility = Visibility.Visible;
                fileTree.BuildRoot();
            }
            else
            {
                inBrowser.Visibility = Visibility.Visible;
                RK.IsEnabled = false;
            }
        }

        #region Private Methods

        //---- Selección de Archivo del TreeView ----//
        private void PopulateFileSelector()
        {
            PopulateFileSelector(fileTree.Selected.ID);
        }

        //---- Selección de Archivos del ItemViewer ----//
        private void PopulateFileSelector(string path)
        {
            try
            {
                var folders = Directory.EnumerateDirectories(path);
                var files = Directory.EnumerateFiles(path);
                FileInfo file;
                items.Clear();
                foreach (string s in folders)
                {
                    AddToSelector(s + "\\", 0);
                }
                foreach (string s in files)
                {
                    file = new FileInfo(s);
                    AddToSelector(s, file.Length);
                }
            }
            catch (Exception)
            {
            }
        }

        //---- 
        private void AddToSelector(string fileName, long size)
        {
            FileItem item = new FileItem();
            string title = GetTitle(fileName);

            item.LiquidTag = fileName;
            //item.MouseRightButtonDown += new MouseButtonEventHandler(item_MouseRightButtonDown);
            item.EditingFinished += new ItemViewerEventHandler(item_EditingFinished);

            if (fileName.EndsWith("\\"))
            {
                item.Icon = "images/large/folder.png";
            }
            else
            {
                item.Icon = "images/large/" + GetIcon(fileName);
                item.OtherText = (Math.Round((double)size / 1024, 2)).ToString() + "KB";
            }

            UpdateTitle(item, title);
            items.Add(item);
        }

        private void UpdateTitle(FileItem item, string title)
        {
            TextBlock titleTemplate = new TextBlock();

            titleTemplate.FontFamily = item.FontFamily;
            titleTemplate.FontSize = item.FontSize;
            titleTemplate.Text = title;

            if (titleTemplate.ActualWidth > 105)
            {
                titleTemplate.Text = string.Empty;
                foreach (char c in title)
                {
                    titleTemplate.Text += c;
                    if (titleTemplate.ActualWidth > 105)
                    {
                        titleTemplate.Text += "..";
                        break;
                    }
                }
            }

            ToolTipService.SetToolTip(item, title);
            item.Text = titleTemplate.Text;
        }

        //---- Obtiene nombre de archivos ----//
        private string GetTitle(string filename)
        {
            string[] split = filename.TrimEnd('\\').Split('\\');
            string title = filename;

            if (split.Length > 0)
            {
                title = split[split.Length - 1];
            }

            return title;
        }

        //---- Obtiene icono de acuerdo a la extensión ----//
        private string GetIcon(string filename)
        {
            string[] split = filename.Split('.');
            string extension = string.Empty;

            if (split.Length > 0)
            {
                extension = split[split.Length - 1].ToLower();
            }

            if (extension != "pdf" && extension != "xls" && extension != "doc" && extension != "gif" && extension != "mp3" &&
                extension != "ascx" && extension != "asmx" && extension != "aspx" && extension != "avi" && extension != "config" &&
                extension != "cs" && extension != "css" && extension != "htm" && extension != "html" && extension != "jpg" &&
                extension != "js" && extension != "mp4" && extension != "png" && extension != "txt" && extension != "xaml" &&
                extension != "xml" && extension != "zip" && extension != "vsd" && extension != "xlsx" && extension != "docx" &&
                extension != "msi" && extension != "db" && extension != "msg" && extension != "ppt" &&
                extension != "png" && extension != "mpp" && extension != "pptx" && extension != "exe")
            {
                extension = "unknown";
            }

            return extension + ".png";
        }

        private int GetChildCount(DirectoryInfo directory)
        {
            int count = 0;

            try
            {
                var folders = directory.EnumerateDirectories();
                var files = directory.EnumerateFiles();
                foreach (DirectoryInfo s in folders)
                {
                    count++;
                }
                foreach (FileInfo s in files)
                {
                    count++;
                }
            }
            catch (Exception)
            {
                count = 0;
            }
            return count;
        }

        //---- Metodo para seleccionar como abrir los archivos ----//
        private void OpenDocument(string documentPath)
        {
            FileInfo fileInfo = new FileInfo(documentPath);

            switch (fileInfo.Extension.Trim('.').ToLower())
            {
                case "doc":
                case "docx":
                case "txt":
                case "ini":
                case "cs":
                case "vb":
                    OpenInWord(fileInfo);
                    break;
                case "xls":
                case "xlsx":
                    OpenInExcel(fileInfo);
                    break;
                case "gif":
                case "jpg":
                case "png":
                    break;
            }

            //filePreview.Visibility = Visibility.Visible;
            //filePreview.Navigate(new Uri(documentPath, UriKind.Absolute));
        }

        //---- Metodo par Abrir archivos en Word ----//
        private void OpenInWord(FileInfo fileInfo)
        {
            dynamic word = AutomationFactory.CreateObject("Word.Application");
            dynamic doc = word.Documents.Open(fileInfo.FullName);
            word.Visible = true;
            doc.Activate();
        }

        //---- Metodo par Abrir archivos en Excel ----//
        private void OpenInExcel(FileInfo fileInfo)
        {
            dynamic excel = AutomationFactory.CreateObject("Excel.Application");
            dynamic doc = excel.Workbooks.Open(fileInfo.FullName);
            excel.Visible = true;
            doc.Activate();
        }

        //---- Excepción al cargar un archivos ----//
        private bool Delete(string filename)
        {
            bool success = true;
            try
            {
                File.Delete(filename);
            }
            catch (Exception)
            {
                //messageBox.ShowAsModal("Se ha producido un error al eliminar el archivo.", "Error");
                success = false;
            }
            PopulateFileSelector();
            return success;
        }
        #endregion

        #region Event Handlers

        //---- Ruta del Arbol a crear ----//
        private void fileTree_Populate(object sender, TreeEventArgs e)
        {
            Node node = (Node)sender;

            if (sender is Tree)
            {	// We are populating the root nodes collection
                fileTree.Nodes.Add(new Node(@"D:\Users\extbinet01\Documents\SharePoint SIOC", "Documentación Sistema SIOC", true, "images/folder.png", "images/folderOpen.png", true));
                fileTree.Nodes[0].Expand();
                fileTree.SetSelected(fileTree.Nodes[0]);
            }
            else
            {	// Otherwise we are populating a node
                PopulateNode(node, e);
            }
        }
        //---- Iconos para el Tree View ----//
        private void PopulateNode(Node node, TreeEventArgs e)
        {
            try
            {
                var folders = Directory.EnumerateDirectories(node.ID);
                var files = Directory.EnumerateFiles(node.ID);
                DirectoryInfo dir;

                foreach (string s in folders)
                {
                    dir = new DirectoryInfo(s);
                    node.Nodes.Add(new Node(s + "\\", dir.Name, (GetChildCount(dir) > 0), "images/folder.png", "images/folderOpen.png", true));
                }
            }
            catch (Exception)
            {
                e.Cancel = true;
            }
        }

        private void fileTree_NodeClick(object sender, TreeEventArgs e)
        {
            if (e.ID.EndsWith("\\"))
            {   // Clicked a folder
                PopulateFileSelector(e.ID);
                image2.Visibility = Visibility.Collapsed;
            }
            else
            {   // Clicked a file
            }
        }

        //---- Metodo para Abrir los archivos al Doble Click ----//
        private void items_DoubleClick(object sender, ItemViewerEventArgs e)
        {
            Node n;
            string selectedFile = items.Selected.LiquidTag.ToString();

            if (fileTree.Selected != null)
            {
                if (selectedFile.EndsWith("\\"))
                {
                    // Double clicked a folder
                    if (!fileTree.Selected.IsExpanded)
                    {
                        fileTree.Selected.Expand();
                    }

                    n = fileTree.Get(selectedFile);
                    fileTree.SetSelected(n);
                }
                else
                {
                    // Double clicked a file
                    OpenDocument(selectedFile);
                }
            }
        }

        private void item_EditingFinished(object sender, ItemViewerEventArgs e)
        {
            if (e.NewTitle != e.Title)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(items.Selected.LiquidTag.ToString());
                    File.Move(fileInfo.FullName, fileInfo.Directory.FullName + "\\" + e.NewTitle);

                    items.Selected.LiquidTag = fileInfo.Directory.FullName + "\\" + e.NewTitle;
                    UpdateTitle((FileItem)items.Selected, e.NewTitle);
                }
                catch (Exception)
                {
                    //messageBox.ShowAsModal("Hubo un cambio de nombre de error en el archivo.", "Error");
                }
                finally
                {
                    e.Cancel = true;
                }
            }
        }

        private void installOutOfBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.InstallState == InstallState.NotInstalled && App.Current.HasElevatedPermissions)
            {
                App.Current.Install();
            }
            else
            {
                //Popup1.IsOpen = true;
                ChildWindow2 mcw = new ChildWindow2();
                if (ckSetShowUpAnimation.IsChecked == false)
                {
                    mcw.Style = (Style)LayoutRoot.Resources["MetroChildWindowStyleCustomAnimation"];
                }
                mcw.Show();
            }
        }

        #endregion

        public partial class App : Application
        {
            public App()
            {
                // ...
                this.CheckAndDownloadUpdateCompleted +=
                new CheckAndDownloadUpdateCompletedEventHandler(CheckUpdates);
            }
            void CheckUpdates(object sender, CheckAndDownloadUpdateCompletedEventArgs e)
            {
                if (e.UpdateAvailable)
                {
                    System.Windows.MessageBox.Show(@"Hay actualizaciones disponibles. Por favor, reinicie la aplicación");
                }
            }
            private void Application_Startup(object sender, StartupEventArgs e)
            {
                this.CheckAndDownloadUpdateAsync();
                this.RootVisual = new MainPage();
            }
            // ...
        }

        private void ActualizarInterfaz()
        {
            if (Application.Current.IsRunningOutOfBrowser)
            {
                installOutOfBrowser.Visibility = Visibility.Collapsed;
                Button.Visibility = Visibility.Visible;
                buttonImage.Visibility = Visibility.Visible;
                Button.IsEnabled = true;
            }
        }

        private void Current_InstallStateChanged(object sender, EventArgs e)
        {
            ActualizarInterfaz();
        }

        //---- Efecto al poner el puntero sobre el control ItemViewer ----//
        private void items_MouseEnter(object sender, MouseEventArgs e)
        {
            items.BorderBrush.Opacity = 4;
            //items.BorderThickness = new Thickness(4);
            image2.Opacity = .40;
        }

        //---- Efecto al quitar puntero sobre el control ItemViewer ----//
        private void items_MouseLeave(object sender, MouseEventArgs e)
        {
            items.BorderBrush.Opacity = 2;
            //items.BorderThickness = new Thickness(2);
            image2.Opacity = 100;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.InstallState == InstallState.NotInstalled)
            {
                //Popup1.IsOpen = true;
                ChildWindow1 mcw = new ChildWindow1();
                if (ckSetShowUpAnimation.IsChecked == false)
                {
                    mcw.Style = (Style)LayoutRoot.Resources["MetroChildWindowStyleCustomAnimation"];
                }
                mcw.Show();
            }
            else
            {
                ChildWindow3 mcw2 = new ChildWindow3();
                if (ckSetShowUpAnimation.IsChecked == false)
                {
                    mcw2.Style = (Style)LayoutRoot.Resources["MetroChildWindowStyleCustomAnimation"];
                }
                mcw2.Show();
            }
        }

        private void btShow_Click(object sender, RoutedEventArgs e)
        {
            ChildWindow window = new ChildWindow();
            //window.Style = (Style)LayoutRoot.Resources["MetroChildWindowStyleCustomAnimation"];
            //window.Show();
            ChildWindow1 mcw = new ChildWindow1();
            if (ckSetShowUpAnimation.IsChecked == false)
            {
                mcw.Style = (Style)LayoutRoot.Resources["MetroChildWindowStyleCustomAnimation"];

            }
            else if (ckSetShowUpAnimation.IsChecked == true)
            {
                //mcw.Style = (Style)LayoutRoot.Resources["MetroChildWindowStyleStandardAnimation"];
            }
            mcw.Show();
        }

        private void installOutOfBrowser_MouseEnter(object sender, MouseEventArgs e)
        {
            installOutOfBrowser.Foreground = new SolidColorBrush(Colors.White);
            installOutOfBrowser.FontSize = 18;
        }

        private void installOutOfBrowser_MouseLeave(object sender, MouseEventArgs e)
        {
            installOutOfBrowser.Foreground = new SolidColorBrush(Colors.White);
            installOutOfBrowser.FontSize = 16;
        }

        protected void Dtimer_Tick(object s, EventArgs args)
        {
            Label2.Content = DateTime.Now;
        }

        private void Label2_MouseEnter(object sender, MouseEventArgs e)
        {
            Label2.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void Label2_MouseLeave(object sender, MouseEventArgs e)
        {
            Label2.Foreground = new SolidColorBrush(Colors.Gray);
        }
 
    }
        
//---- Muestra Pop Up con Información ----//
        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    //Popup1.IsOpen = true;
        //    MetroChildWindow mcw = new MetroChildWindow();
        //    if (ckSetShowUpAnimation.IsChecked == false)
        //    {
        //        mcw.Style = (Style)LayoutRoot.Resources["MetroChildWindowStyleCustomAnimation"];
        //    }
        //    mcw.Show();
        //}

        //protected void Dtimer_Tick(object s, EventArgs args)
        //{
        //    Label2.Content = DateTime.Now;
        //}

        //private void Label2_MouseEnter(object sender, MouseEventArgs e)
        //{
        //    Label2.Foreground = new SolidColorBrush(Colors.Black);
        //}

        //private void Label2_MouseLeave(object sender, MouseEventArgs e)
        //{
        //    Label2.Foreground = new SolidColorBrush(Colors.Gray);
        //}

        //private void btShow_Click(object sender, RoutedEventArgs e)
        //{
        //    //ChildWindow window = new ChildWindow();
        //    //window.Style = (Style)LayoutRoot.Resources["MetroChildWindowStyleCustomAnimation"];
        //    //window.Show();
        //    //MetroChildWindow mcw = new MetroChildWindow();
        //    if (ckSetShowUpAnimation.IsChecked == false)
        //    {
        //        mcw.Style = (Style)LayoutRoot.Resources["MetroChildWindowStyleCustomAnimation"];
                
        //    }
        //    else if (ckSetShowUpAnimation.IsChecked == true)
        //    {
        //        //mcw.Style = (Style)LayoutRoot.Resources["MetroChildWindowStyleStandardAnimation"];
        //    }
        //    mcw.Show();
        //}
    } 