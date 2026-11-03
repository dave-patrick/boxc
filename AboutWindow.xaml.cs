using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BoxC;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var asm = Assembly.GetExecutingAssembly();
        var v = asm.GetName().Version;
        ReleaseText.Text = v is null ? "?" : $"{v.Major}.{v.Minor}.{v.Build}";
        BuildText.Text = asm.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "BuildId")?.Value ?? "?";

        Loaded += (_, _) => { Topmost = true; Activate(); Topmost = false; };
    }
}
