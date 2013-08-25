Imports System.Management
Imports System.Threading
Imports System.Globalization
Imports Serializer

Class Application
    Shared Property Config As ConfigApp
    Sub New()
        ' Dim Culture As CultureInfo = New CultureInfo("fr-FR")
        ' Thread.CurrentThread.CurrentCulture = Culture
        ' Thread.CurrentThread.CurrentUICulture = Culture
        Config = ConfigApp.LoadConfig()
    End Sub
    ' Les événements de niveau application, par exemple Startup, Exit et DispatcherUnhandledException
    ' peuvent être gérés dans ce fichier. 
    Private Sub Application_DispatcherUnhandledException(ByVal sender As Object, ByVal e As System.Windows.Threading.DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
        CType(MainWindow, MainWindow).ForcageArretApplication()
        MsgBox(e.Exception.TargetSite.ToString & Chr(13) & e.Exception.Message & Chr(13), , "Erreur fatale")
    End Sub

    Protected Overrides Sub Finalize()
        ConfigApp.SaveConfig(Config)
        MyBase.Finalize()
    End Sub
End Class
