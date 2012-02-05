using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Shell;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Phone.Tasks;


namespace Tomatoes
{
   
    public partial class MainPage : PhoneApplicationPage
    {
        #region Private Variables
        List<System.Windows.Point> _tappedPoints;
        List<Image> _tomatoImages;
        List<Image> _splatTomatoImages;

        PageOrientation _captureOrientation;
        
        WriteableBitmap _writableBitmapOfScreen;

        int _tapCount = 0;
        int _tomatodirection = 4;

        CaptureSource _capture;
        TransformGroup _tg = new TransformGroup();
        ScaleTransform _st = new ScaleTransform();
        RotateTransform _rt = new RotateTransform();
        bool _isVideoLive = true;
        bool _isSoundLive = true;

        #endregion

        // Constructor
        public MainPage()
        {
            #region Hack to work around capabilities bug
            Microphone mic = Microphone.Default;
            if (mic.State == MicrophoneState.Started)
            {
            }
            #endregion

            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
            this.OrientationChanged += new EventHandler<OrientationChangedEventArgs>(MainPage_OrientationChanged);
            
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (_splatTomatoImages != null && _capture != null)
            {
                this.ClearScreen();
            }
            base.OnNavigatedTo(e);
        }

        #region Event Handlers

        private void MainPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {

            if (_isVideoLive)
            {

                if (Orientation == PageOrientation.LandscapeRight)
                {
                    _rt.Angle = 180;
                    _rt.CenterX = this.rectVideo.ActualWidth * 0.5;
                    _rt.CenterY = this.rectVideo.ActualHeight * 0.5;
                }

                if (Orientation == PageOrientation.LandscapeLeft)
                {
                    _rt.Angle = 0;
                    _rt.CenterX = this.rectVideo.ActualWidth * 0.5;
                    _rt.CenterY = this.rectVideo.ActualHeight * 0.5;
                }
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _st.ScaleX = 1;
            _st.ScaleY = 1;

            _tg.Children.Add(_st);
            _tg.Children.Add(_rt);
            
            this.rectVideo.RenderTransform = _tg;

            InitializeCamera();

            ApplicationBar = new ApplicationBar();
            ApplicationBar.IsVisible = true;
            ApplicationBar.IsMenuEnabled = true;
            ApplicationBar.BackgroundColor = Colors.Gray;

            ApplicationBarIconButton clearButton = new ApplicationBarIconButton(new Uri("CleanUp.png", UriKind.Relative));
            clearButton.Text = "Clean Up";
            clearButton.Click += new EventHandler(ClearButton_Click);
            ApplicationBar.Buttons.Add(clearButton);

            ApplicationBarIconButton capture = new ApplicationBarIconButton(new Uri("CameraIcon.png", UriKind.Relative));
            capture.Text = "Capture";
            capture.Click += new EventHandler(CaptureButton_Click);
            ApplicationBar.Buttons.Add(capture);

            ApplicationBarIconButton sound = new ApplicationBarIconButton(new Uri("SoundIcon.png", UriKind.Relative));
            sound.Text = "Mute";
            sound.Click += new EventHandler(SoundButton_Click);
            ApplicationBar.Buttons.Add(sound);

            ApplicationBarMenuItem reviewmyapp = new ApplicationBarMenuItem("Rate this app");
            reviewmyapp.Click += new EventHandler(reviewmyapp_Click);
            ApplicationBar.MenuItems.Add(reviewmyapp);

            _tappedPoints = new List<System.Windows.Point>();
            _tomatoImages = new List<Image>();
            _splatTomatoImages = new List<Image>();

            _tapCount = 0;

            this.ClearScreen();

            Microsoft.Phone.Shell.SystemTray.IsVisible = false;

        }

        private void reviewmyapp_Click(object sender, EventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
            marketplaceReviewTask.Show();
        }

        private void SoundButton_Click(object sender, EventArgs e)
        {
            _isSoundLive = !_isSoundLive;
            if (!_isSoundLive)
            {
                muteImage.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                muteImage.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void CaptureButton_Click(object sender, EventArgs e)
        {
            this.LayoutRoot.Tap -= LayoutRoot_Tap;

            _capture.CaptureImageAsync();

            _captureOrientation = Orientation;
            _isVideoLive = false;
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            this.LayoutRoot.Tap -= LayoutRoot_Tap;

            ClearScreen();

            this.LayoutRoot.Tap += new EventHandler<GestureEventArgs>(LayoutRoot_Tap);
            _isVideoLive = true;
        } 

        private void LayoutRoot_Tap(object sender, GestureEventArgs e)
        {
            this.LayoutRoot.Tap -= LayoutRoot_Tap;

            this.PlayAudioFile("ThrowAudio.wav");

            Image tomatoImage = new Image();
            tomatoImage.Source = new BitmapImage(new Uri("tomato.png", UriKind.RelativeOrAbsolute));
            tomatoImage.Height = 74;
            tomatoImage.Width = 63;
            TomatoRedirection(tomatoImage);
            _tomatoImages.Add(tomatoImage);

            LayoutRoot.Children.Add(tomatoImage);

            System.Windows.Point tappedHere = e.GetPosition(this.LayoutRoot);
            _tappedPoints.Add(tappedHere);
            _tapCount++;


            // Create a duration of 0.5 seconds.
            Duration duration = new Duration(TimeSpan.FromMilliseconds(400));

            // Create two DoubleAnimations and set their properties.
            DoubleAnimation myDoubleAnimation1 = new DoubleAnimation();
            DoubleAnimation myDoubleAnimation2 = new DoubleAnimation();

            myDoubleAnimation1.Duration = duration;
            myDoubleAnimation2.Duration = duration;

            Storyboard sb = new Storyboard();
            sb.Duration = duration;

            sb.Children.Add(myDoubleAnimation1);
            sb.Children.Add(myDoubleAnimation2);

            Storyboard.SetTarget(myDoubleAnimation1, tomatoImage);
            Storyboard.SetTarget(myDoubleAnimation2, tomatoImage);

            // Set the attached properties of Canvas.Left and Canvas.Top
            // to be the target properties of the two respective DoubleAnimations.
            Storyboard.SetTargetProperty(myDoubleAnimation1, new PropertyPath("(Canvas.Left)"));
            Storyboard.SetTargetProperty(myDoubleAnimation2, new PropertyPath("(Canvas.Top)"));

            myDoubleAnimation1.To = tappedHere.X;//widhth 63 
            myDoubleAnimation2.To = tappedHere.Y; //Height 74


            ScaleTransform scale = new ScaleTransform();
            tomatoImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            tomatoImage.RenderTransform = scale;

            DoubleAnimation growAnimationX = new DoubleAnimation();
            growAnimationX.Duration = TimeSpan.FromMilliseconds(300);
            growAnimationX.From = 5;
            growAnimationX.To = 1;

            DoubleAnimation growAnimationY = new DoubleAnimation();
            growAnimationY.Duration = TimeSpan.FromMilliseconds(300);
            growAnimationY.From = 5;
            growAnimationY.To = 1;

            Storyboard.SetTargetProperty(growAnimationX, new PropertyPath("(Image.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(growAnimationX, tomatoImage);



            Storyboard.SetTargetProperty(growAnimationY, new PropertyPath("(Image.RenderTransform).(ScaleTransform.ScaleY)"));
            Storyboard.SetTarget(growAnimationY, tomatoImage);

            sb.Children.Add(growAnimationX);
            sb.Children.Add(growAnimationY);

            // Begin the animation.
            sb.Begin();

            sb.Completed += new EventHandler(sb_Completed);
        }

        private void sb_Completed(object sender, EventArgs e)
        {
            this.LayoutRoot.Tap +=LayoutRoot_Tap;

            (sender as Storyboard).Completed -= sb_Completed;

            this.PlayAudioFile("SplatAudio.wav");

            Image splatImageLocal = new Image();
            splatImageLocal.Source = new BitmapImage(new Uri("TomatoSplat.png", UriKind.RelativeOrAbsolute));
            splatImageLocal.Height = 114;
            splatImageLocal.Width = 124;
            if (_tappedPoints.Count == _tapCount && _tomatoImages.Count == _tapCount)
            {
                Canvas.SetLeft(splatImageLocal, _tappedPoints[_tapCount - 1].X - 20);
                Canvas.SetTop(splatImageLocal, _tappedPoints[_tapCount - 1].Y);

                _tomatoImages[_tapCount - 1].Opacity = 0;
                splatImageLocal.Opacity = 1;

                LayoutRoot.Children.Add(splatImageLocal);
                _splatTomatoImages.Add(splatImageLocal);
            }

        }

        private void _capture_CaptureImageCompleted(object sender, CaptureImageCompletedEventArgs e)
        {
            PageOrientation temp = Orientation;

            this.rectVideo.Fill = new ImageBrush { ImageSource = e.Result };

            if (muteImage.Visibility == System.Windows.Visibility.Visible)
            {
                //remove it from canvas before capturing the layout
                muteImage.Visibility = System.Windows.Visibility.Collapsed;
            }
            LayOutToBitMapImage(); //as the layout has the imagebrushcapture and the images layed on top of it.  

            if (!_isSoundLive)
            {
                //if sound is Muted enable the MuteImage icon
                muteImage.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void capture_Tap(object sender, GestureEventArgs e)
        {
            this.LayoutRoot.Tap -= LayoutRoot_Tap;

            _capture.CaptureImageAsync();
        }

        #endregion

        #region Private Methods

        private void InitializeCamera()
        {
            if (_capture == null)
            {
                _capture = new CaptureSource();
                _capture.VideoCaptureDevice = CaptureDeviceConfiguration.GetDefaultVideoCaptureDevice();
                _capture.AudioCaptureDevice = null;
                _capture.CaptureImageCompleted += new EventHandler<CaptureImageCompletedEventArgs>(_capture_CaptureImageCompleted);
            }

            if (_capture != null)
            {
                _capture.Stop();

                _capture.VideoCaptureDevice = CaptureDeviceConfiguration.GetDefaultVideoCaptureDevice();
                _capture.AudioCaptureDevice = null;

                VideoBrush videoBrush = new VideoBrush();
                videoBrush.Stretch = Stretch.Fill;
                videoBrush.SetSource(_capture);
                rectVideo.Fill = videoBrush;

                if ((CaptureDeviceConfiguration.AllowedDeviceAccess || CaptureDeviceConfiguration.RequestDeviceAccess()))
                {
                    _capture.Start();
                }

            }
        }
       
        private void ClearScreen()
        {
            _isVideoLive = true;
            for (int i = 0; i < this._splatTomatoImages.Count; i++)
            {
                if (this.LayoutRoot.Children.Contains(_splatTomatoImages[i]))
                {
                    this.LayoutRoot.Children.Remove(_splatTomatoImages[i]);
                }
            }

            for (int i = 0; i < this._tomatoImages.Count; i++)
            {
                if (this.LayoutRoot.Children.Contains(_tomatoImages[i]))
                {
                    this.LayoutRoot.Children.Remove(_tomatoImages[i]);
                }
            }

            _splatTomatoImages.Clear();
            _tomatoImages.Clear();
            _tappedPoints.Clear();
            _tapCount = 0;

            this.InitializeCamera();

        }

        private void TomatoRedirection(Image tomatoImage)
        {

            if (_tomatodirection == 4)
            {
                Canvas.SetLeft(tomatoImage, -66);
                Canvas.SetTop(tomatoImage, 459);
                _tomatodirection = 3;
                return;
            }

            if (_tomatodirection == 3)
            {
                Canvas.SetLeft(tomatoImage, -71);
                Canvas.SetTop(tomatoImage, -77);
                _tomatodirection = 2;
                return;
            }
            if (_tomatodirection == 2)
            {
                Canvas.SetLeft(tomatoImage, 736);
                Canvas.SetTop(tomatoImage, -74);
                _tomatodirection = 1;
                return;
            }
            if (_tomatodirection == 1)
            {
                Canvas.SetLeft(tomatoImage, 735);
                Canvas.SetTop(tomatoImage, 474);
                _tomatodirection = 4;
                return;
            }
        }

        private void PlayAudioFile(string media)
        {
            if (_isSoundLive)
            {
                var stream = TitleContainer.OpenStream(media);
                var effect = SoundEffect.FromStream(stream);
                FrameworkDispatcher.Update();
                effect.Play();
            }
        }

        private void LayOutToBitMapImage()
        {

            // create a WriteableBitmap
            _writableBitmapOfScreen = new WriteableBitmap(
                (int)this.LayoutRoot.ActualWidth,
                (int)this.LayoutRoot.ActualHeight);

            // render the visual element to the WriteableBitmap
            _writableBitmapOfScreen.Render(this.LayoutRoot, new TranslateTransform());

            // request an redraw of the bitmap
            _writableBitmapOfScreen.Invalidate();

            // Get an Image Stream
            MemoryStream ms_Image = new MemoryStream();

            // write the image into the stream
            System.Windows.Media.Imaging.Extensions.SaveJpeg(_writableBitmapOfScreen, ms_Image, _writableBitmapOfScreen.PixelWidth, _writableBitmapOfScreen.PixelHeight, 0, 100);
            // reset the stream pointer to the beginning

            ms_Image.Seek(0, 0);

            //JpegInfo info = ExifReader.ReadJpeg(ms_Image, "tomatoes");
            if (_captureOrientation == PageOrientation.LandscapeLeft)
            {
                Stream temp = RotateStream(ms_Image, 90);
                temp.Seek(0, 0);
                ms_Image.Close();

                var library = new MediaLibrary();
                library.SavePictureToCameraRoll("tomato", temp);
                temp.Close();

            }
            else
            {
                Stream temp = RotateStream(ms_Image, 270);
                temp.Seek(0, 0);
                ms_Image.Close();

                var library = new MediaLibrary();
                library.SavePictureToCameraRoll("tomato", temp);
                temp.Close();
            }

        }

        /*Got this piece from http://timheuer.com/blog/archive/2010/09/23/working-with-pictures-in-camera-tasks-in-windows-phone-7-orientation-rotation.aspx*/
        private Stream RotateStream(Stream stream, int angle)
        {
            stream.Position = 0;
            if (angle % 90 != 0 || angle < 0) throw new ArgumentException();
            if (angle % 360 == 0) return stream;

            BitmapImage bitmap = new BitmapImage();
            bitmap.SetSource(stream);
            WriteableBitmap wbSource = new WriteableBitmap(bitmap);

            WriteableBitmap wbTarget = null;
            if (angle % 180 == 0)
            {
                wbTarget = new WriteableBitmap(wbSource.PixelWidth, wbSource.PixelHeight);
            }
            else
            {
                wbTarget = new WriteableBitmap(wbSource.PixelHeight, wbSource.PixelWidth);
            }

            for (int x = 0; x < wbSource.PixelWidth; x++)
            {
                for (int y = 0; y < wbSource.PixelHeight; y++)
                {
                    switch (angle % 360)
                    {
                        case 90:
                            wbTarget.Pixels[(wbSource.PixelHeight - y - 1) + x * wbTarget.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                        case 180:
                            wbTarget.Pixels[(wbSource.PixelWidth - x - 1) + (wbSource.PixelHeight - y - 1) * wbSource.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                        case 270:
                            wbTarget.Pixels[y + (wbSource.PixelWidth - x - 1) * wbTarget.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                    }
                }
            }
            MemoryStream targetStream = new MemoryStream();
            wbTarget.SaveJpeg(targetStream, wbTarget.PixelWidth, wbTarget.PixelHeight, 0, 100);
            return targetStream;
        }

        #endregion

    }
}