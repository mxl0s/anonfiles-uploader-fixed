﻿Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Text.RegularExpressions


Public Class Form1



    Public MoveForm As Boolean
    Public MouseX, MouseY As Integer
    '//-------------------------------------------------------
    Private Items As New Dictionary(Of WebClient, ListViewItem)
    Private Files As New ArrayList


    Private Sub GetURI(ByVal Fichier As String)
       Try
            IM.Images.Add(Icon.ExtractAssociatedIcon(Fichier))
            Dim NewItem As New ListViewItem
            NewItem.UseItemStyleForSubItems = False
            NewItem.Text = Path.GetFileName(Fichier)
            NewItem.SubItems.Add("Getting Data...").ForeColor = Color.Yellow
            NewItem.ImageIndex = IM.Images.Count - 1
            Dim Info As New FIleInfo
            NewItem.Tag = Info
            Lv1.Items.Add(NewItem)
            Using WC As New WebClient
                WC.Encoding = Encoding.UTF8
                AddHandler WC.DownloadStringCompleted, AddressOf COMPLETE
                WC.DownloadStringTaskAsync(New Uri("https://anonfiles.com"))
                Items.Add(WC, NewItem)
                Files.Add(Fichier)
            End Using
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub COMPLETE(sender As Object, e As DownloadStringCompletedEventArgs)
        On Error Resume Next
        If e.Cancelled = True Then
            Items(sender).SubItems(1).Text = "Cancelled..."
            Items(sender).SubItems(1).ForeColor = Color.Orange
        ElseIf e.Error IsNot Nothing Then
            Items(sender).SubItems(1).Text = "Error..."
            Items(sender).SubItems(1).ForeColor = Color.Red
        ElseIf e.Result IsNot Nothing Then
            Dim URL As String = "https://api.anonfiles.com/upload"
            Dim token As String = Regex.Match(e.Result, " {'token': '(.*?)'}").Groups(1).Value
            Using WC As New WebClient

                WC.Headers.Add("api", "https://api.anonfiles.com/upload")
                WC.Headers.Add("token", token)
                WC.Headers.Add("requested-with", "XMLHttpRequest")

                AddHandler WC.UploadProgressChanged, AddressOf LOADING
                AddHandler WC.UploadFileCompleted, AddressOf COMPLETE

                WC.UploadFileTaskAsync(New Uri(URL), Files(Items(sender).Index))

                Dim NewItem As ListViewItem = Items(sender)
                Items.Remove(sender)
                Items.Add(WC, NewItem)

            End Using
        Else
            Items(sender).SubItems(1).Text = "Error..."
        End If
    End Sub

    Private Sub LOADING(sender As Object, e As UploadProgressChangedEventArgs)
        On Error Resume Next
        Items(sender).SubItems(1).Text = "uploading: (" & FileSize(e.BytesSent) & "/" & FileSize(e.TotalBytesToSend) & ")..."
    End Sub

    Private Sub COMPLETE(sender As Object, e As UploadFileCompletedEventArgs)
        Try
            If e.Cancelled = True Then
                Items(sender).SubItems(1).Text = "Cancelled..."
                Items(sender).SubItems(1).ForeColor = Color.OrangeRed
            ElseIf e.Error IsNot Nothing Then
                Items(sender).SubItems(1).Text = "Error..."
                Items(sender).SubItems(1).ForeColor = Color.Red
            ElseIf e.Result IsNot Nothing Then
                Dim Result As String = Encoding.UTF8.GetString(e.Result)
                If Result.Contains("{""status"":true,") Then
                    Items(sender).Tag.SetInfo(Result)
                    Items(sender).SubItems(1).Text = "Done."
                    Items(sender).SubItems(1).ForeColor = Color.Green
                ElseIf Result.Contains("file too big") Then
                    Items(sender).SubItems(1).Text = "max size 20gb"
                    Items(sender).SubItems(1).ForeColor = Color.DarkOrange
                End If
            End If
        Catch ex As Exception
            MsgBox("Error : " & ex.Message)
        End Try

    End Sub

#Region "DRAG & DROP , FILE SIZE"



    Private Function FileSize(ByVal Tamanho As Double) As String
        Dim Tipos As String() = {"B", "KB", "MB", "GB"}
        Dim TamanhoDouble As Double = Tamanho
        Dim CSA As Integer = 0
        While TamanhoDouble >= 1024 AndAlso CSA + 1 < Tipos.Length
            CSA += 1
            TamanhoDouble = TamanhoDouble / 1024
        End While
        Return [String].Format("{0:0.##} {1}", TamanhoDouble, Tipos(CSA))
    End Function

    Private Sub Form1_DragDrop(sender As Object, e As DragEventArgs) Handles Me.DragDrop, Lv1.DragDrop
        For Each X In e.Data.GetData(DataFormats.FileDrop)
            If File.Exists(X) Then
                GetURI(X)
            ElseIf Directory.Exists(X) Then
                For Each O In Directory.GetFiles(X, "*.*", SearchOption.AllDirectories)
                    GetURI(O)
                Next
            End If
        Next
    End Sub

    Private Sub Form1_DragEnter(sender As Object, e As DragEventArgs) Handles Me.DragEnter, Lv1.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.All
        End If
    End Sub

#End Region

    Private Sub CopyFullLinkToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CopyFullLinkToolStripMenuItem.Click
        On Error Resume Next
        Lv1.FocusedItem.Tag.GetClipboard("FULL")
    End Sub

    Private Sub CopyShortLinkToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CopyShortLinkToolStripMenuItem.Click
        On Error Resume Next
        Lv1.FocusedItem.Tag.GetClipboard("SHORT")
    End Sub

#Region "Form Moving"

    Private Sub Panel1_MouseDown(sender As Object, e As MouseEventArgs) Handles Panel1.MouseDown, lb1.MouseDown, P1.MouseDown
        MoveForm = True
        MouseX = Cursor.Position.X - Me.Left
        MouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub Panel1_MouseMove(sender As Object, e As MouseEventArgs) Handles Panel1.MouseMove, lb1.MouseMove, P1.MouseMove
        If MoveForm Then
            MoveForm = True
            Me.Left = Cursor.Position.X - MouseX
            Me.Top = Cursor.Position.Y - MouseY
        End If
    End Sub

    Private Sub Panel1_MouseUp(sender As Object, e As MouseEventArgs) Handles Panel1.MouseUp, lb1.MouseUp, P1.MouseMove
        MoveForm = False
    End Sub

#End Region



    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click
        End
    End Sub

    Private Sub lb1_Click(sender As Object, e As EventArgs) Handles lb1.Click

    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Sub CM1_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles CM1.Opening
        If Lv1.Items.Count = 0 Then
            CopyFullLinkToolStripMenuItem.Enabled = False
            CopyShortLinkToolStripMenuItem.Enabled = False
        ElseIf Lv1.FocusedItem.SubItems(1).Text.Contains("Done") Then
            CopyFullLinkToolStripMenuItem.Enabled = True
            CopyShortLinkToolStripMenuItem.Enabled = True
        End If

    End Sub
End Class
