using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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



namespace Swis.WpfDebugger
{
	/// <summary>
	 /// Class used to have an image that is able to be gray when the control is not enabled.
	 /// Based on the version by Thomas LEBRUN (http://blogs.developpeur.org/tom)
	 /// </summary>
	public class AutoGrayableImage : Image
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AutoGrayableImage"/> class.
		/// </summary>
		static AutoGrayableImage()
		{
			// Override the metadata of the IsEnabled and Source property.
			IsEnabledProperty.OverrideMetadata(typeof(AutoGrayableImage), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnAutoGreyScaleImageIsEnabledPropertyChanged)));
			SourceProperty.OverrideMetadata(typeof(AutoGrayableImage), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnAutoGreyScaleImageSourcePropertyChanged)));
		}

		protected static AutoGrayableImage GetImageWithSource(DependencyObject source)
		{
			var image = source as AutoGrayableImage;
			if (image == null)
				return null;

			if (image.Source == null)
				return null;

			return image;
		}

		/// <summary>
		/// Called when [auto grey scale image source property changed].
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		protected static void OnAutoGreyScaleImageSourcePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs ars)
		{
			AutoGrayableImage image = GetImageWithSource(source);
			if (image != null)
				ApplyGreyScaleImage(image, image.IsEnabled);
		}

		/// <summary>
		/// Called when [auto grey scale image is enabled property changed].
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		protected static void OnAutoGreyScaleImageIsEnabledPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
		{
			AutoGrayableImage image = GetImageWithSource(source);
			if (image != null)
			{
				var isEnabled = Convert.ToBoolean(args.NewValue);
				ApplyGreyScaleImage(image, isEnabled);
			}
		}

		protected static void ApplyGreyScaleImage(AutoGrayableImage autoGreyScaleImg, Boolean isEnabled)
		{
			try
			{
				if (!isEnabled)
				{
					BitmapSource bitmapImage = null;

					if (autoGreyScaleImg.Source is FormatConvertedBitmap)
					{
						// Already grey !
						return;
					}
					else if (autoGreyScaleImg.Source is BitmapSource)
					{
						bitmapImage = (BitmapSource)autoGreyScaleImg.Source;
					}
					else // trying string 
					{
						bitmapImage = new BitmapImage(new Uri(autoGreyScaleImg.Source.ToString()));
					}

					FormatConvertedBitmap conv = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray8, null, 0);

					autoGreyScaleImg.Source = conv;

					// Create Opacity Mask for greyscale image as FormatConvertedBitmap does not keep transparency info
					autoGreyScaleImg.OpacityMask = new ImageBrush(((FormatConvertedBitmap)autoGreyScaleImg.Source).Source); //equivalent to new ImageBrush(bitmapImage)
					autoGreyScaleImg.Opacity = 0.25;
				}
				else
				{
					if (autoGreyScaleImg.Source is FormatConvertedBitmap)
					{
						autoGreyScaleImg.Source = ((FormatConvertedBitmap)autoGreyScaleImg.Source).Source;
					}
					else if (autoGreyScaleImg.Source is BitmapSource)
					{
						// Should be full color already.
						return;
					}

					// Reset the Opcity Mask
					autoGreyScaleImg.OpacityMask = null;
					autoGreyScaleImg.Opacity = 1.0;
				}
			}
			catch (Exception)
			{
				// nothin'
			}

		}

	}
}