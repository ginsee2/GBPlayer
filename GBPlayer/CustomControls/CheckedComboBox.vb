

Imports System.Windows.Controls.Primitives


Public Class CheckedComboBox
    Inherits ComboBox
    Private ItemToDisplay As New ComboBoxItem
    Shared Sub New()
        'Cet appel OverrideMetadata indique au système que cet élément souhaite apporter un style différent de celui de sa classe de base.
        'Ce style est défini dans themes\generic.xaml
        DefaultStyleKeyProperty.OverrideMetadata(GetType(CheckedComboBox), New FrameworkPropertyMetadata(GetType(CheckedComboBox)))
    End Sub
    Sub New()
        MyBase.New()
        Me.AddHandler(CheckBox.ClickEvent, New RoutedEventHandler(AddressOf chkSelect_Click))
    End Sub
    Private Sub chkSelect_Click(ByVal Sender As Object, ByVal e As RoutedEventArgs)
        Dim TextToDisplay = ""
        Debug.Print("passage click")
        Debug.Print(e.OriginalSource.GetType.Name)
        If TypeOf (e.OriginalSource) Is CheckBox Then
            For Each item As ComboBoxItem In Me.Items
                Dim BorderSelect = CType(item.Template.FindName("BorderSelect", item), Border)
                If BorderSelect IsNot Nothing Then
                    Dim chkSelect = CType(BorderSelect.FindName("chkSelect"), CheckBox)
                    If chkSelect IsNot Nothing Then
                        If chkSelect.IsChecked.GetValueOrDefault Then
                            If TextToDisplay <> "" Then
                                TextToDisplay += ", "
                            End If
                            TextToDisplay += chkSelect.Content
                        End If
                    End If
                End If
            Next
            If ItemToDisplay IsNot Nothing Then
                If Me.Items.Contains(ItemToDisplay) Then Me.Items.Remove(ItemToDisplay)
                ItemToDisplay = New ComboBoxItem()
                ItemToDisplay.Content = TextToDisplay
                ItemToDisplay.Visibility = Visibility.Collapsed
                Me.Items.Add(ItemToDisplay)
                Me.SelectedItem = ItemToDisplay
            End If
        End If
    End Sub
End Class
