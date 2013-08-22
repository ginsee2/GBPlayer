Imports System.Windows.Controls.Primitives

Public Class GridTextBox
    Inherits TextBox

    Public Shared ReadOnly IsEnableLinqProperty As DependencyProperty = DependencyProperty.Register("IsEnableLinq",
        GetType(Boolean), GetType(GridTextBox), New UIPropertyMetadata(False, Nothing))
    Public Property IsEnableLinq() As Boolean
        Get
            Return DirectCast(GetValue(IsEnableLinqProperty), Boolean)
        End Get
        Set(ByVal value As Boolean)
            SetValue(IsEnableLinqProperty, value)
        End Set
    End Property

    Shared Sub New()
        'Cet appel OverrideMetadata indique au système que cet élément souhaite apporter un style différent de celui de sa classe de base.
        'Ce style est défini dans themes\generic.xaml
        DefaultStyleKeyProperty.OverrideMetadata(GetType(GridTextBox), New FrameworkPropertyMetadata(GetType(GridTextBox)))
    End Sub

    Public Shared ReadOnly LinqEvent As RoutedEvent = EventManager.RegisterRoutedEvent("Linq", RoutingStrategy.Direct, GetType(RoutedEventHandler), GetType(GridTextBox))
    Public Custom Event Linq As RoutedEventHandler
        AddHandler(ByVal value As RoutedEventHandler)
            Me.AddHandler(LinqEvent, value)
        End AddHandler

        RemoveHandler(ByVal value As RoutedEventHandler)
            Me.RemoveHandler(LinqEvent, value)
        End RemoveHandler

        RaiseEvent(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Me.RaiseEvent(e)
        End RaiseEvent
    End Event

    Protected Overrides Sub OnPreviewMouseLeftButtonDown(ByVal e As System.Windows.Input.MouseButtonEventArgs)
        Dim newEventArgs As New RoutedEventArgs(GridTextBox.LinqEvent)
        If (Text <> "") And (IsEnableLinq) Then
            Dim Item As ListViewItem = WpfApplication.FindAncestor(e.Source, "ListViewItem")
            If Not Item.IsSelected Then
                MyBase.RaiseEvent(newEventArgs)
            End If
        End If
    End Sub
End Class
