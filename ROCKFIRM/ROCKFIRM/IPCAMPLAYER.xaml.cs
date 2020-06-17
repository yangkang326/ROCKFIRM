using ROCKFIRM.Device;
using ROCKFIRM.Media;
using ROCKFIRM.PTZ;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Vlc.DotNet.Core;
using Vlc.DotNet.Forms;
using System.Windows.Markup;
using Button = System.Windows.Controls.Button;

namespace ROCKFIRM
{
    /// <summary>
    /// IPCAMPLAYER.xaml 的交互逻辑
    /// </summary>
    public partial class IPCAMPLAYER : System.Windows.Controls.UserControl
    {
        public string ipad { set; get; }
        public VlcMediaPlayer mediaplayer
        {
            get;
            set;
        }
        public VlcControl control = new VlcControl();

        private Profile[] profiles;

        private MediaClient mediaClient;

        private Uri playingrui;

        private string svpath;

        private Thread th;

		private Thread th1;
		private Thread th2;

		private bool yorn;

        private string iptemp;
		int posr, posc, gridindex;
        public IPCAMPLAYER()
        {
			InitializeComponent();
			videoplayer.Child = control;
			var currentAssembly = Assembly.GetEntryAssembly();
			var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
			var libDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
			control.BeginInit();
			control.VlcLibDirectory = libDirectory;
			control.EndInit();

		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			iptemp = address.Text;
			th1 = new Thread(new ThreadStart(contipcamera));
			th1.Start();

		}

		private void contipcamera()
        {
			try
			{
				Device.DeviceClient deviceClient = GetDeviceClient(iptemp);
				Device.Service[] services = deviceClient.GetServices(false);
				Device.Service xmedia = services.FirstOrDefault(s => s.Namespace == "http://www.onvif.org/ver10/media/wsdl");
				if (xmedia != null)
				{
					mediaClient = GetMediaClient();

					profiles = GetProfiles(mediaClient);
					Media.StreamSetup streamSetup = GetstreamSetup(mediaClient, profiles);
					if (profiles != null)
					{
						foreach (var p in profiles)
						{
							if (combobox.Items.Contains(p.Name))
								break;
							else
								combobox.Items.Add(p.Name);
						}
					}
				}
			}
			catch
			{
				th1.Abort();
			}
			combobox.SelectionChanged += new SelectionChangedEventHandler(listBox_SelectionChanged);
			
		}


		private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			bool flag = profiles != null && combobox.SelectedIndex >= 0;
			if (flag)
			{
				StreamSetup streamSetup = new StreamSetup();
				streamSetup.Stream = StreamType.RTPUnicast;
				streamSetup.Transport = new Transport();
				streamSetup.Transport.Protocol = TransportProtocol.RTSP;
				MediaUri mediaUri = new MediaUri();
				mediaUri = mediaClient.GetStreamUri(streamSetup, profiles[combobox.SelectedIndex].token);
				UriBuilder uriBuilder = new UriBuilder(mediaUri.Uri);
				uriBuilder.Scheme = "rtsp";
				string[] options = new string[]
				{
					":rtsp-http",
					":rtsp-http-port=" + uriBuilder.Port.ToString(),
					":rtsp-user=admin",
					":rtsp-pwd=admin123",
					":network-caching=300"
				};
				control.Play(uriBuilder.Uri, options);
				stop_play.IsEnabled = true;
				playingrui = uriBuilder.Uri;
			}
		}

		private UriBuilder GetUri(string ip_port)
		{
			UriBuilder uriBuilder = new UriBuilder("http:/onvif/device_service");
			string[] array = ip_port.Split(new char[]
			{
				':'
			});
			uriBuilder.Host = array[0];
			return new UriBuilder(array[0])
			{
				Port = 8899
			};
		}

		private DeviceClient GetDeviceClient(string ip_port)
		{
			UriBuilder uri = GetUri(ip_port);
			HttpTransportBindingElement httpTransportBindingElement = new HttpTransportBindingElement();
			httpTransportBindingElement.AuthenticationScheme = AuthenticationSchemes.Digest;
			httpTransportBindingElement.KeepAliveEnabled = false;
			TextMessageEncodingBindingElement textMessageEncodingBindingElement = new TextMessageEncodingBindingElement();
			System.ServiceModel.Channels.Binding binding = new CustomBinding(new BindingElement[]
			{
				new TextMessageEncodingBindingElement(MessageVersion.Soap12WSAddressing10, Encoding.UTF8),
				httpTransportBindingElement
			});
			return new DeviceClient(binding, new EndpointAddress(uri.ToString()));
		}

		private Profile[] GetProfiles(MediaClient mediaClient)
		{
			return mediaClient.GetProfiles();
		}

		private StreamSetup GetstreamSetup(MediaClient mediaClient, Profile[] profiles)
		{
			Media.StreamSetup streamSetup = new Media.StreamSetup();
			streamSetup.Stream = Media.StreamType.RTPUnicast;
			streamSetup.Transport = new Media.Transport();
			streamSetup.Transport.Protocol = Media.TransportProtocol.RTSP;
			return streamSetup;
		}

		private void location_save(object sender, RoutedEventArgs e)
		{
			string empty = string.Empty;
			FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
			bool flag = folderBrowserDialog.ShowDialog() == DialogResult.OK;
			if (flag)
			{
				location.Text = folderBrowserDialog.SelectedPath;
				svpath = location.Text;
			}
		}

		private void test(object sender, RoutedEventArgs e)
		{
			control.Stop();
			stop_play.IsEnabled = false;
			combobox.SelectedIndex = -1;
		}

		private PTZClient GetPTZClient()
		{
			EndpointAddress remoteAddress = new EndpointAddress(string.Format("http://{0}:{1}/onvif/ptz_service", address.Text, 8899));
			HttpTransportBindingElement httpTransportBindingElement = new HttpTransportBindingElement
			{
				AuthenticationScheme = AuthenticationSchemes.Digest
			};
			TextMessageEncodingBindingElement textMessageEncodingBindingElement = new TextMessageEncodingBindingElement
			{
				MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None)
			};
			CustomBinding binding = new CustomBinding(new BindingElement[]
			{
				textMessageEncodingBindingElement,
				httpTransportBindingElement
			});
			PTZClient pTZClient = new PTZClient(binding, remoteAddress);
			PasswordDigestBehavior item = new PasswordDigestBehavior("admin", "admin123");
			pTZClient.Endpoint.Behaviors.Add(item);
			return pTZClient;
		}

		private MediaClient GetMediaClient()
		{
			EndpointAddress remoteAddress = new EndpointAddress(string.Format("http://{0}:{1}/onvif/Media", address.Text, 8899));
			HttpTransportBindingElement httpTransportBindingElement = new HttpTransportBindingElement();
			httpTransportBindingElement.AuthenticationScheme = AuthenticationSchemes.Digest;
			httpTransportBindingElement.KeepAliveEnabled = false;
			TextMessageEncodingBindingElement textMessageEncodingBindingElement = new TextMessageEncodingBindingElement
			{
				MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None)
			};
			CustomBinding binding = new CustomBinding(new BindingElement[]
			{
				textMessageEncodingBindingElement,
				httpTransportBindingElement
			});
			MediaClient mediaClient = new MediaClient(binding, remoteAddress);
			PasswordDigestBehavior item = new PasswordDigestBehavior("admin", "admin123");
			mediaClient.Endpoint.Behaviors.Add(item);
			return mediaClient;
		}

		private void zoomout(object sender, RoutedEventArgs e)
		{
			bool flag = combobox.Items.Count > 0 && combobox.SelectedIndex != 2;
			if (flag)
			{
				PTZClient pTZClient = GetPTZClient();
				PTZConfigurationOptions configurationOptions = pTZClient.GetConfigurationOptions(profiles[combobox.SelectedIndex].PTZConfiguration.token);
				ROCKFIRM.PTZ.PTZSpeed velocity = new ROCKFIRM.PTZ.PTZSpeed
				{
					Zoom = new ROCKFIRM.PTZ.Vector1D
					{
						x = -0.5f
					}
				};
				pTZClient.ContinuousMove(profiles[combobox.SelectedIndex].token, velocity, null);
			}
		}

		private void zoomin(object sender, RoutedEventArgs e)
		{
			bool flag = combobox.Items.Count > 0 && combobox.SelectedIndex != 2;
			if (flag)
			{
				PTZClient pTZClient = GetPTZClient();
				PTZConfigurationOptions configurationOptions = pTZClient.GetConfigurationOptions(profiles[combobox.SelectedIndex].PTZConfiguration.token);
				ROCKFIRM.PTZ.PTZSpeed velocity = new ROCKFIRM.PTZ.PTZSpeed
				{
					Zoom = new ROCKFIRM.PTZ.Vector1D
					{
						x = 0.5f
					}
				};
				pTZClient.ContinuousMove(profiles[combobox.SelectedIndex].token, velocity, null);
			}
		}

		private void Stop_zoom(object sender, RoutedEventArgs e)
		{
			bool flag = combobox.Items.Count > 0 && combobox.SelectedIndex != 2;
			if (flag)
			{
				PTZClient pTZClient = GetPTZClient();
				PTZConfigurationOptions configurationOptions = pTZClient.GetConfigurationOptions(profiles[combobox.SelectedIndex].PTZConfiguration.token);
				pTZClient.Stop(profiles[combobox.SelectedIndex].token, true, true);
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			address.Text = ipad;
			svpath = location.Text;
			th2 = new Thread(new ThreadStart(checkcontrolisplaying));
			th2.Start();
		}

		private void checkcontrolisplaying()
        {
			recordcheckbox.Dispatcher.Invoke(new Action(() =>
		   {
			   recordcheckbox.IsEnabled = control.IsPlaying;
		   }));
        }

		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			yorn = true;
			th = new Thread(new ThreadStart(ThreadMethod));
			th.Start();
		}

		private void ThreadMethod()
		{
			string directoryName = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			DirectoryInfo vlcLibDirectory = new DirectoryInfo(System.IO.Path.Combine(directoryName, "libvlc", (IntPtr.Size == 4) ? "win-x86" : "win-x64"));
			string path = System.DateTime.Now.ToString("yyyy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss") + ".ts";
			string str = System.IO.Path.Combine(svpath, path);
			mediaplayer = new VlcMediaPlayer(vlcLibDirectory);
			bool isPlaying = control.IsPlaying; ;
			while (isPlaying)
			{
				string[] options = new string[]
				{
					":sout=#file{dst=" + str + "}",
					":sout-keep"
				};
				mediaplayer.SetMedia(playingrui, options);
				bool flag = yorn;
				if (flag)
				{
					mediaplayer.Play();
				}
				else
				{
					th.Interrupt();
				}
				isPlaying = control.IsPlaying;
			}
			mediaplayer.Stop();
			th.Interrupt();
		}

		private void torecord_Unchecked(object sender, RoutedEventArgs e)
		{
			yorn = false;
			mediaplayer.Stop();
		}

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
			th.Abort();
			th1.Abort();
			
        }

        private void resize(object sender, RoutedEventArgs e)
        {
			Button button = sender as Button;
			DependencyObject sp2 = VisualTreeHelper.GetParent(button);
			DependencyObject sp1 = VisualTreeHelper.GetParent(sp2);
			DependencyObject sp0 = VisualTreeHelper.GetParent(sp1);
			DependencyObject sp = VisualTreeHelper.GetParent(sp0);
			DependencyObject spa = VisualTreeHelper.GetParent(sp);
			DependencyObject spc = VisualTreeHelper.GetParent(spa);
			if (Grid.GetRowSpan((IPCAMPLAYER)spc)!=1 & Grid.GetColumnSpan((IPCAMPLAYER)spc) != 1)
            {
				Grid.SetColumn((IPCAMPLAYER)spc, posc);
				Grid.SetRow((IPCAMPLAYER)spc, posr);
				Grid.SetColumnSpan((IPCAMPLAYER)spc, 1);
				Grid.SetRowSpan((IPCAMPLAYER)spc, 1);
				Grid.SetZIndex((IPCAMPLAYER)spc, 0);
			}
			
		}

        private void allscrean(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
			DependencyObject sp2 = VisualTreeHelper.GetParent(button);
			DependencyObject sp1 = VisualTreeHelper.GetParent(sp2);
			DependencyObject sp0 = VisualTreeHelper.GetParent(sp1);
			DependencyObject sp = VisualTreeHelper.GetParent(sp0);
			DependencyObject spa = VisualTreeHelper.GetParent(sp);
			DependencyObject spc = VisualTreeHelper.GetParent(spa);
			if (Grid.GetRowSpan((IPCAMPLAYER)spc) == 1 & Grid.GetColumnSpan((IPCAMPLAYER)spc) == 1)
			{
			 	gridindex = Grid.GetZIndex((IPCAMPLAYER)spc);
				posr = Grid.GetRow((IPCAMPLAYER)spc);
				posc = Grid.GetColumn((IPCAMPLAYER)spc);
				Grid.SetColumn((IPCAMPLAYER)spc, 0);
				Grid.SetRow((IPCAMPLAYER)spc, 0);
				Grid.SetColumnSpan((IPCAMPLAYER)spc, 2);
				Grid.SetRowSpan((IPCAMPLAYER)spc, 2);
				Grid.SetZIndex((IPCAMPLAYER)spc, 1);
			}
		}
    }
}
