using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IsoSplitProject.Converter
{
    public class InverseBoolToOpacityConverter : IValueConverter
	{
		#region Fields
		private bool _not = false;
		#endregion
		public bool Not { get { return _not; } set { _not = value; } }

		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (Not)
			{
				if (!(bool)value)
				{
					return 0.1;
				}
				else
				{
					double val = 1;

					if (parameter != null)
					{
						double.TryParse(parameter.ToString(), out val);
					}

					return val;
				}
			}
			else
			{
				if ((bool)value)
				{
					return 0.1;
				}
				else
				{
					double val = 1;

					if (parameter != null)
					{
						double.TryParse(parameter.ToString(), out val);
					}

					return val;
				}
			}


		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
