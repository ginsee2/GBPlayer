

Imports System.Windows.Controls.Primitives

Public Class ScrollViewerAlwaysScroll
    Inherits ScrollViewer
    Shared Sub New()
        'Cet appel OverrideMetadata indique au système que cet élément souhaite apporter un style différent de celui de sa classe de base.
        'Ce style est défini dans themes\generic.xaml
        DefaultStyleKeyProperty.OverrideMetadata(GetType(ScrollViewerAlwaysScroll), New FrameworkPropertyMetadata(GetType(ScrollViewerAlwaysScroll)))
    End Sub
    Sub New()
        MyBase.New()
        Me.AddHandler(ScrollViewer.MouseWheelEvent, New RoutedEventHandler(AddressOf ViewerMouseWheel), True)
    End Sub
    Private Sub ViewerMouseWheel(ByVal sender As Object, ByVal e As RoutedEventArgs)
        Dim eargs As MouseWheelEventArgs = DirectCast(e, MouseWheelEventArgs)
        Dim x As Double = CDbl(eargs.Delta)
        Dim y As Double = Me.VerticalOffset
        Me.ScrollToVerticalOffset(y - x)
        e.Handled = True
    End Sub
End Class
