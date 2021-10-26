using System.Threading.Tasks;

namespace DataWF.Common
{
    /// <summary>
    /// https://github.com/xamarin/Essentials/blob/master/Xamarin.Essentials/Clipboard/Clipboard.shared.cs
    /// await WPF Implementation
    /// </summary>
    public interface IClipboard
    {
        Task SetTextAsync(string text);
        Task SetHtmlAsync(string html, string text);
        
        bool HasText { get; }

        Task<string> GetTextAsync();
    }
}