using System.ComponentModel.DataAnnotations;
using System.Globalization;
namespace ConcurrentTransactions.API.Util
{

    /// <summary>
    /// A validator that is used to ascertain that
    /// a Payment uses the correct monetary code
    /// </summary>
    public class ISOCurrencyValidator : ValidationAttribute
    {

        private static readonly HashSet<string> IsoCurrencyCodes = InitializeCurrencyCodes();
        private static HashSet<string> InitializeCurrencyCodes()
        {
            var currencyCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                try
                {
                    var region = new RegionInfo(culture.LCID);
                    currencyCodes.Add(region.ISOCurrencySymbol);
                }
                catch
                {
                }
            }

            return currencyCodes;
        }

        /// <summary>
        ///Ascertains if the inserted currency standard is a real one.
        ///The hashmap
        /// </summary>
        /// <returns></returns>
        public override bool IsValid(object? currencyValue)
        {

            if (currencyValue is not string strValue || string.IsNullOrWhiteSpace(strValue))
            {
                return false;
            }

            return IsoCurrencyCodes.Contains(strValue.Trim());

        }
    }
}
