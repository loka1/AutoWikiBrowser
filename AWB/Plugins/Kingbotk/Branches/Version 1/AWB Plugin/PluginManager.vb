Imports System.Xml.Serialization

Namespace AutoWikiBrowser.Plugins.SDKSoftware.Kingbotk
    ''' <summary>
    ''' The plugin manager, which interracts with AWB and manages the individual plugins
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class PluginManager
        ' Fields here shouldn't need to be Shared, as there will only ever be one instance - created by AWB at startup
        Implements IAWBPlugin

        Friend Const conWikiPlugin As String = "[[User:Kingbotk/P|Plugin]]"
        Friend Const conWikiPluginBrackets As String = "(" & conWikiPlugin & ") "
        Friend Const conTextBoxInsertionMenuName As String = "insertTagToolStripMenuItem"
        Friend Const ENWiki As String = "http://en.wikipedia.org/wiki/"
        Private Const conMe As String = "Plugin manager"

        ' Regular expressions:
        Private ReqPhotoNoParamsRegex As New Regex("\{\{\s*(template\s*:\s*|)\s*reqphoto[\s\n\r]*\}\}[\s\n\r]*", _
           RegexOptions.IgnoreCase Or RegexOptions.Compiled Or RegexOptions.ExplicitCapture)

        ' Plugins:
        Private ActivePlugins As New List(Of PluginBase)
        Private Plugins As New Dictionary(Of String, PluginBase)
        Private WithEvents AssessmentsObject As Assessments

        'AWB objects:
        Friend Shared AWBForm As IAWBMainForm
        Friend Shared listmaker As WikiFunctions.Lists.ListMaker
        Private contextmenu As ContextMenuStrip, tabpages As TabControl.TabPageCollection
        Private WithEvents webcontrol As WikiFunctions.Browser.WebControl
        Friend Shared WithEvents StatusText As New ToolStripStatusLabel("Initialising plugin")
        Private AWBPluginsMenu As ToolStripMenuItem
        Private AWBTextBox As TextBox

        ' Menu items:
        Private WithEvents AddGenericTemplateMenuItem As New ToolStripMenuItem("Add Generic Template")
        Private WithEvents NudgeMenuItem As New ToolStripMenuItem("Nudge AWB if it freezes")
        Private WithEvents MenuShowHide As New ToolStripMenuItem("Show settings tabs")

        ' Library state and shared objects:
        Private PluginsTab As New TabPage("Plugin")
        Private LoggingTab As New TabPage("Logging")
        Private PluginSettings As PluginSettingsControl
        Private SettingsTabs As New List(Of TabPage)
        Private WithEvents BotModeCheckbox As CheckBox

        ' User settings:
        Private blnShowManualAssessmentsInstructions As Boolean = True
        Private Shared mUserName As String, mLoggedIn As Boolean

        ' AWB events
        Private Event Diff() Implements WikiFunctions.Plugin.IAWBPlugin.Diff
        Private Event Save() Implements WikiFunctions.Plugin.IAWBPlugin.Save
        Private Event Skip() Implements WikiFunctions.Plugin.IAWBPlugin.Skip
        Private Event Start() Implements WikiFunctions.Plugin.IAWBPlugin.Start
        Private Event [Stop]() Implements WikiFunctions.Plugin.IAWBPlugin.Stop
        Private Event Preview() Implements WikiFunctions.Plugin.IAWBPlugin.Preview

        ' Friend events
        Friend Event AWBBotModeCheckboxCheckedChange(ByVal Checked As Boolean)

        ' SkipReason:
        Private Enum SkipReason As Byte
            Other
            BadNamespace
            ProcessingMainArticleDoesntExist
            ProcessingTalkPageArticleDoesntExist
        End Enum

        ' Bot timer:
        Private WithEvents BotTimer As Timer
        Private blnHaveHeardFromAWB As Boolean, blnWeStoppedAWB As Boolean

        ' XML:
        Private Const conShowHideTabsParm As String = "ShowHideTabs"
        Private Const conShowManualAssessmentsInstructions As String = "ShowManualAssessmentsInstructions"
        Private Const conGenericTemplatesCount As String = "GenericTemplatesCount"
        Private Const conGenericTemplate As String = "GenericTemplate"
        Private Const conNudgeParm As String = "Nudge"

        ' AWB interface:
        Private ReadOnly Property Name() As String Implements WikiFunctions.Plugin.IAWBPlugin.Name
            Get
                Return "Kingbotk Plugin"
            End Get
        End Property
        Private Sub Initialise(ByVal list As WikiFunctions.Lists.ListMaker, ByVal web As WikiFunctions.Browser.WebControl, _
        ByVal tsmi As ToolStripMenuItem, ByVal cms As ContextMenuStrip, ByVal tab As TabControl, ByVal frm As Form, _
        ByVal txt As TextBox) Implements WikiFunctions.Plugin.IAWBPlugin.Initialise

            ' Set to en-US for logging:
            System.Threading.Thread.CurrentThread.CurrentCulture = New Globalization.CultureInfo("en-US")

            ' Store AWB object references:
            AWBForm = DirectCast(frm, IAWBMainForm)
            webcontrol = web
            contextmenu = cms
            listmaker = list
            tabpages = tab.TabPages
            AWBTextBox = txt
            AWBPluginsMenu = tsmi

            ' Initialise our settings object:
            PluginSettings = New PluginSettingsControl(txt, AWBForm.BotModeCheckbox, ActivePlugins)

            With AWBForm
                ' Set up our UI objects:
                BotModeCheckbox = .BotModeCheckbox ' WithEvents
                .StatusStrip.Items.Insert(2, StatusText)
                StatusText.Margin = New Padding(50, 0, 50, 0)
                StatusText.BorderSides = ToolStripStatusLabelBorderSides.Left Or ToolStripStatusLabelBorderSides.Right
                StatusText.BorderStyle = Border3DStyle.Etched
                .HelpToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() _
                   {PluginSettings.MenuHelp, PluginSettings.MenuHelpReleaseNotes, PluginSettings.MenuAbout})
            End With

            ' UI - addhandlers for Start/Stop/Diff/Preview/Save/Ignore buttons/form closing:
            AddHandler frm.FormClosing, AddressOf Me.AWBClosingEventHandler

            With PluginSettings
                ' Raise the AWB events when user clicks one of our buttons:
                AddHandler .btnDiff.Click, AddressOf Me.OurButtonsClickEventHander
                AddHandler .btnStop.Click, AddressOf Me.OurButtonsClickEventHander
                AddHandler .btnStart.Click, AddressOf Me.OurButtonsClickEventHander
                AddHandler .btnPreview.Click, AddressOf Me.OurButtonsClickEventHander
                AddHandler .btnSave.Click, AddressOf Me.OurButtonsClickEventHander
                AddHandler .btnIgnore.Click, AddressOf Me.OurButtonsClickEventHander
            End With
            With AWBForm
                AddHandler .SkipButton.Click, AddressOf PluginSettings.AWBSkipButtonClickEventHandler
                ' Get notification when AWB buttons enabled-state changes:
                AddHandler .DiffButton.EnabledChanged, AddressOf PluginSettings.AWBButtonsEnabledHandler
                AddHandler .StopButton.EnabledChanged, AddressOf PluginSettings.AWBButtonsEnabledHandler
                AddHandler .StartButton.EnabledChanged, AddressOf PluginSettings.AWBButtonsEnabledHandler
                AddHandler .PreviewButton.EnabledChanged, AddressOf PluginSettings.AWBButtonsEnabledHandler
                AddHandler .SaveButton.EnabledChanged, AddressOf PluginSettings.AWBButtonsEnabledHandler
                AddHandler .SkipButton.EnabledChanged, AddressOf PluginSettings.AWBButtonsEnabledHandler
                ' AWB Stop button click (since webcontrol.busy still seems flaky) :
                AddHandler PluginSettings.btnStop.Click, AddressOf Me.StopButtonClickEventHandler
                AddHandler .StopButton.Click, AddressOf Me.StopButtonClickEventHandler
            End With

            ' Add handler to get username from AWB:
            AddHandler webcontrol.Loaded, AddressOf webcontrol_Loaded
            AddHandler webcontrol.Diffed, AddressOf webcontrol_Loaded
            AddHandler webcontrol.None, AddressOf webcontrol_Loaded

            ' Track Manual Assessment checkbox:
            AddHandler PluginSettings.ManuallyAssessCheckBox.CheckedChanged, _
               AddressOf Me.ManuallyAssessCheckBox_CheckChanged

            ' Initialise enabled state of our replica buttons:
            PluginSettings.AWBButtonsEnabledHandler(AWBForm.StartTab.Controls("btnDiff"), Nothing)
            PluginSettings.AWBButtonsEnabledHandler(AWBForm.StartTab.Controls("btnStop"), Nothing)
            PluginSettings.AWBButtonsEnabledHandler(AWBForm.StartTab.Controls("btnStart"), Nothing)
            PluginSettings.AWBButtonsEnabledHandler(AWBForm.StartTab.Controls("btnPreview"), Nothing)
            PluginSettings.AWBButtonsEnabledHandler(AWBForm.SaveButton, Nothing)
            PluginSettings.AWBButtonsEnabledHandler(AWBForm.SkipButton, Nothing)

            ' Replicate the AWB article stats labels:
            For Each lbl As Label In AWBForm.StartTab.Controls("groupBox3").Controls
                AddHandler lbl.TextChanged, AddressOf PluginSettings.AWBArticleStatsLabelChangeEventHandler
            Next

            ' Tabs:
            PluginsTab.UseVisualStyleBackColor = True
            PluginsTab.Controls.Add(PluginSettings)
            LoggingTab.UseVisualStyleBackColor = True
            LoggingTab.Controls.Add(PluginSettings.LoggingSettings)

            ' Show/hide tabs menu:
            With MenuShowHide
                .CheckOnClick = True
                .Checked = True
            End With
            DirectCast(tsmi.Owner.Items("ToolStripMenuGeneral"), _
               ToolStripMenuItem).DropDownItems.Add(MenuShowHide)

            ' Add-Generic-Template menu:
            tsmi.DropDownItems.Add(AddGenericTemplateMenuItem)

            ' Universal context menus:
            'AddItemToTextBoxInsertionContextMenu(PluginSettings.TextInsertContextMenuStrip.Items) ' wasn't working (nor was iteration)
            AddItemToTextBoxInsertionContextMenu(PluginSettings.ClassToolStripMenuItem, txt)
            AddItemToTextBoxInsertionContextMenu(PluginSettings.ImportanceToolStripMenuItem, txt)
            AddItemToTextBoxInsertionContextMenu(PluginSettings.PriorityToolStripMenuItem, txt)

            ' Create plugins:
            Plugins.Add("Albums", New WPAlbums(Me))
            Plugins.Add("Australia", New WPAustralia(Me))
            'Plugins.Add("Film", New Film(Me))
            Plugins.Add("India", New WPIndia(Me))
            Plugins.Add("MilHist", New WPMilHist(Me))
            Plugins.Add("Songs", New WPSongs(Me))
            Plugins.Add("WPNovels", New WPNovels(Me))
            Plugins.Add("WPBiography", New WPBiography(Me))
            ' hopefully if add WPBio last it will ensure that the template gets added to the *top* of pages

            ' Initialise plugins:
            For Each plugin As KeyValuePair(Of String, PluginBase) In Plugins
                plugin.Value.Initialise(tsmi, txt)
            Next

            ' Add our menu items last:
            With NudgeMenuItem
                .CheckOnClick = True
                .Checked = True
                .Enabled = False
                .ToolTipText = "Nudge AWB if it freezes when in auto (bot) mode"
            End With
            PluginSettings.PluginToolStripMenuItem.DropDownItems.Add(NudgeMenuItem)
            tsmi.DropDownItems.Add(PluginSettings.PluginToolStripMenuItem)

            ' Reset statusbar text:
            DefaultStatusText()
        End Sub
        Private Sub LoadSettings(ByVal Prefs() As Object) Implements WikiFunctions.Plugin.IAWBPlugin.LoadSettings
            If Prefs.Length > 0 Then
                ' Check if we're receiving an old type settings block (XMLTextReader) or new (a serialized string)
                If Prefs(0).GetType Is GetType(XmlTextReader) Then
                    ReadXML(DirectCast(Prefs(0), XmlTextReader))
                ElseIf Prefs(0).GetType Is GetType(String) Then
                    LoadSettingsNewWay(CType(Prefs(0), String))
                End If
            End If
        End Sub
        Private Function ProcessArticle(ByVal ArticleText As String, ByVal ArticleTitle As String, _
        ByVal NS As Integer, ByRef Summary As String, ByRef Skip As Boolean) As String _
        Implements WikiFunctions.Plugin.IAWBPlugin.ProcessArticle
            If ActivePlugins.Count = 0 Then Return ArticleText

            Dim TheArticle As Article, Namesp As Namespaces = DirectCast(NS, Namespaces)

            PluginSettings.Led1.Colour = Colour.Green
            StatusText.Text = "Processing " & ArticleTitle
            PluginSettingsControl.MyTrace.ProcessingArticle(ArticleTitle, Namesp)

            blnHaveHeardFromAWB = True

            For Each p As PluginBase In ActivePlugins
                If Not p.IAmReady Then
                    MessageBox.Show("The generic template plugin """ & p.conPluginShortName & _
                       """isn't properly configured.", "Can't start", MessageBoxButtons.OK, _
                       MessageBoxIcon.Error)
                    StopAWB()
                    GoTo SkipOrStop
                End If
            Next

            Select Case Namesp
                Case Namespaces.Main
                    If PluginSettings.ManuallyAssess Then
                        If webcontrol.ArticlePageExists Then
                            StatusText.Text += ": Click Preview to read the article; " & _
                               "click Save or Ignore to load the assessments form"
                            AssessmentsObject.ProcessMainSpaceArticle(ArticleTitle)
                            Summary = "Clean up"
                            GoTo SkipOrStop
                        Else
                            ProcessArticle = Skipping(Summary, "", _
                               SkipReason.ProcessingMainArticleDoesntExist, ArticleText, Skip)
                            GoTo ExitMe
                        End If
                    Else
                        GoTo SkipBadNamespace
                    End If

                Case Namespaces.Talk
                    If webcontrol.ArticlePageExists Then
                        Dim ReqPhoto As Boolean

                        If Not webcontrol.TalkPageExists Then PluginSettings.PluginStats.NewArticles += 1

                        TheArticle = New Article(ArticleText, ArticleTitle, Namesp)

                        If Not PluginSettings.ManuallyAssess Then _
                           TheArticle.PluginManagerEditSummaryTaggingCategory(PluginSettings.CategoryName)

                        For Each p As PluginBase In ActivePlugins
                            If p.HasReqPhotoParam Then
                                With TheArticle
                                    If ReqPhotoNoParamsRegex.IsMatch(.AlteredArticleText) Then
                                        .AlteredArticleText = ReqPhotoNoParamsRegex.Replace(.AlteredArticleText, "")
                                        .DoneReplacement("{{reqphoto}}", "template param(s)", True, conMe)
                                        .ArticleHasAMajorChange()
                                        ReqPhoto = True
                                        Exit For
                                    End If
                                End With
                            End If
                        Next

                        If PluginSettings.ManuallyAssess Then
                            If Not AssessmentsObject.ProcessTalkPage(TheArticle, PluginSettings, ActivePlugins, Me, _
                            ReqPhoto) Then
                                Skip = True
                                GoTo SkipOrStop
                            End If
                        Else
                            For Each p As PluginBase In ActivePlugins
                                p.ProcessTalkPage(TheArticle, ReqPhoto)
                                If TheArticle.PluginManagerGetSkipResults = SkipResults.SkipBadTag Then _
                                   Exit For
                            Next
                        End If

                        ProcessArticle = FinaliseArticleProcessing(TheArticle, Skip, Summary, ArticleText)
                    Else
                        ProcessArticle = Skipping(Summary, "", SkipReason.ProcessingTalkPageArticleDoesntExist, _
                           ArticleText, Skip, ArticleTitle, Namespaces.Talk)
                    End If

                Case Namespaces.CategoryTalk, Namespaces.ImageTalk, Namespaces.PortalTalk, _
                   Namespaces.ProjectTalk, Namespaces.TemplateTalk
                    If PluginSettings.ManuallyAssess Then
                        MessageBox.Show("The plugin has received a non-standard namespace talk page in " & _
                           "manual assessment mode. Please remove this item from the list and start again.", _
                           "Manual Assessments", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        StopAWB()
                        GoTo SkipOrStop
                    Else
                        TheArticle = New Article(ArticleText, ArticleTitle, Namesp)

                        TheArticle.PluginManagerEditSummaryTaggingCategory(PluginSettings.CategoryName)
                        For Each p As PluginBase In ActivePlugins
                            p.ProcessTalkPage(TheArticle, Classification.Code, Importance.NA, False, _
                               False, False, PluginBase.ProcessTalkPageMode.NonStandardTalk, False)
                            If TheArticle.PluginManagerGetSkipResults = SkipResults.SkipBadTag Then _
                               Exit For
                        Next

                        ProcessArticle = FinaliseArticleProcessing(TheArticle, Skip, Summary, ArticleText)
                    End If

                Case Else
                    GoTo SkipBadNamespace
            End Select

TempHackInsteadOfDefaultSettings:
            If Not Skip AndAlso AWBForm.EditSummary.Text = "clean up" Then AWBForm.EditSummary.Text = "Tagging"

ExitMe:
            If Not PluginSettings.ManuallyAssess Then DefaultStatusText()
            PluginSettingsControl.MyTrace.Flush()
            PluginSettings.Led1.Colour = Colour.Red
            Exit Function

SkipBadNamespace:
            ProcessArticle = Skipping(Summary, "", SkipReason.BadNamespace, ArticleText, Skip)
            GoTo ExitMe

SkipOrStop:
            ProcessArticle = ArticleText
            GoTo ExitMe
        End Function
        Private Sub Reset() Implements WikiFunctions.Plugin.IAWBPlugin.Reset
            blnShowManualAssessmentsInstructions = True
            NudgeMenuItem.Checked = True
            With PluginSettings
                .Reset()
                .SkipBadTags = BotMode
                .SkipWhenNoChange = BotMode
            End With
            For Each plugin As KeyValuePair(Of String, PluginBase) In Plugins
                plugin.Value.Reset()
            Next
        End Sub
        Private Function SaveSettings() As Object() Implements WikiFunctions.Plugin.IAWBPlugin.SaveSettings
            Dim st As New IO.StringWriter
            Dim Writer As New System.Xml.XmlTextWriter(st)

            Writer.WriteStartElement("root")
            WriteXML(Writer)
            Writer.WriteEndElement()
            Writer.Flush()

            Return New Object() {st.ToString}
        End Function

        ' Private routines:
        Private Function FinaliseArticleProcessing(ByVal TheArticle As Article, ByRef Skip As Boolean, _
        ByRef Summary As String, ByVal ArticleText As String) As String

            If TheArticle.PluginManagerGetSkipResults = SkipResults.NotSet Then
                PluginSettings.PluginStats.Tagged += 1
            Else
                With PluginSettings.PluginStats
                    Select Case TheArticle.PluginManagerGetSkipResults
                        Case SkipResults.SkipBadTag ' always skip
                            If PluginSettings.SkipBadTags Then
                                .SkippedBadTagIncrement()
                                Skip = True ' always skip
                            Else
                                ' the plugin manager stops processing when it gets a bad tag. We know however
                                ' that one plugin found a bad template and possibly replaced it with
                                ' conTemplatePlaceholder. We're also not skipping, so we need to remove the placeholder
                                TheArticle.AlteredArticleText = _
                                   TheArticle.AlteredArticleText.Replace(PluginBase.conTemplatePlaceholder, "")
                                MessageBox.Show("Bad tag. Please fix it manually or click ignore.", "Bad tag", _
                                   MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                PluginSettings.PluginStats.Tagged += 1
                                ' Return false (don't skip)
                            End If
                        Case SkipResults.SkipMiscellaneous, SkipResults.SkipNoChange
                            If TheArticle.ProcessIt Then
                                .Tagged += 1
                                ' Return false (don't skip)
                            Else
                                If TheArticle.PluginManagerGetSkipResults = SkipResults.SkipMiscellaneous Then
                                    .SkippedMiscellaneousIncrement()
                                    Skip = True ' skip
                                Else
                                    If PluginSettings.SkipWhenNoChange Then
                                        .SkippedNoChangeIncrement()
                                        Skip = True ' skip
                                    Else
                                        PluginSettings.PluginStats.Tagged += 1
                                        ' Return false (don't skip)
                                    End If
                                End If
                            End If
                    End Select
                End With
            End If

            If Skip Then
                Return Skipping(Summary, TheArticle.EditSummary, SkipReason.Other, ArticleText, Skip)
            Else
                TheArticle.PluginManagerLookForHeaderTemplatesAndFinaliseEditSummary(webcontrol)
                PluginSettingsControl.MyTrace.WriteArticleActionLine1("Returning to AWB: Edit summary: " & _
                   TheArticle.EditSummary, conMe, True)
                FinaliseArticleProcessing = TheArticle.AlteredArticleText
                Summary = TheArticle.EditSummary
            End If
        End Function
        Private Function Skipping(ByRef EditSummary As String, ByVal DefaultEditSummary As String, _
        ByVal SkipReason As SkipReason, ByVal ArticleText As String, ByRef Skip As Boolean, _
        Optional ByVal ArticleTitle As String = Nothing, Optional ByVal NS As Namespaces = Namespaces.Talk) _
        As String

            If BotMode Then EditSummary = "This article should have been skipped" _
               Else EditSummary = DefaultEditSummary

            Select Case SkipReason
                Case PluginManager.SkipReason.BadNamespace
                    PluginSettings.PluginStats.SkippedNamespaceIncrement()
                    PluginSettingsControl.MyTrace.SkippedArticle(conMe, "Incorrect namespace")
                Case PluginManager.SkipReason.ProcessingMainArticleDoesntExist
                    PluginSettings.PluginStats.SkippedRedLinkIncrement()
                    PluginSettingsControl.MyTrace.SkippedArticle(conMe, "Article doesn't exist")
                Case PluginManager.SkipReason.ProcessingTalkPageArticleDoesntExist
                    PluginSettings.PluginStats.SkippedRedLinkIncrement()
                    PluginSettingsControl.MyTrace.SkippedArticleRedlink(ArticleTitle, NS)
                Case PluginManager.SkipReason.Other
                    PluginSettingsControl.MyTrace.SkippedArticle(conMe, "")
            End Select

            Skip = True
            Return ArticleText
        End Function
        Private Sub CreateNewGenericPlugin(ByVal PluginName As String, ByVal Creator As String)
            Dim plugin As GenericTemplatePlugin

            PluginSettingsControl.MyTrace.WriteBulletedLine(Creator & ": Creating generic template """ & _
               PluginName & """", True, True)

            plugin = New GenericTemplatePlugin(Me, PluginName)
            Plugins.Add(PluginName, plugin)
            plugin.Initialise(AWBPluginsMenu, AWBTextBox)
            plugin.Enabled = True ' (adds it to activeplugins)
        End Sub
        Private Sub TurnNudgesOn()
            BotTimer = New Timer
            With BotTimer
                .Interval = 600000
                .Enabled = True
            End With
        End Sub
        Private Sub TurnNudgesOff()
            If BotTimer Is Nothing Then Exit Sub

            BotTimer.Enabled = False
            BotTimer = Nothing
        End Sub
        Private Sub RemoveLoggingTab()
            If tabpages.Contains(LoggingTab) Then tabpages.Remove(LoggingTab)
        End Sub

        ' Friend interface exposed to client plugins:
        Friend Sub ShowHidePluginTab(ByVal tabp As TabPage, ByVal Visible As Boolean)
            If Visible Then
                If MenuShowHide.Checked Then
                    If Not tabpages.Contains(tabp) Then
                        Dim ContainedMainTab As Boolean = tabpages.Contains(PluginsTab)
                        If ContainedMainTab Then
                            RemoveLoggingTab()
                            tabpages.Remove(PluginsTab)
                        End If
                        SettingsTabs.Add(tabp)
                        tabpages.Add(tabp)
                        If ContainedMainTab Then
                            tabpages.Add(LoggingTab)
                            tabpages.Add(PluginsTab)
                        End If
                    End If
                End If
            Else
                If tabpages.Contains(tabp) Then
                    SettingsTabs.Remove(tabp)
                    tabpages.Remove(tabp)
                End If
            End If
        End Sub
        Friend Sub PluginEnabledStateChanged(ByVal Plugin As PluginBase, ByVal IsEnabled As Boolean)
            If IsEnabled Then
                If Not ActivePlugins.Contains(Plugin) Then
                    ActivePlugins.Add(Plugin)
                    If ActivePlugins.Count = 1 Then
                        If MenuShowHide.Checked Then tabpages.Add(LoggingTab)
                        tabpages.Add(PluginsTab)
                    End If
                End If
            Else
                ActivePlugins.Remove(Plugin)
                If ActivePlugins.Count = 0 Then
                    RemoveLoggingTab()
                    tabpages.Remove(PluginsTab)
                End If
            End If
            DefaultStatusText()
        End Sub
        Friend ReadOnly Property BotMode() As Boolean
            Get
                Return AWBForm.BotModeCheckbox.Checked
            End Get
        End Property
        Friend Sub StopAWB()
            RaiseEvent [Stop]()
            blnWeStoppedAWB = True
        End Sub
        Friend Sub DeleteGenericPlugin(ByVal PG As IGenericTemplatePlugin, ByVal P As PluginBase)
            PG.Goodbye(AWBPluginsMenu)
            Plugins.Remove(PG.GenericTemplateKey)
            If ActivePlugins.Contains(P) Then ActivePlugins.Remove(P)
            If ActivePlugins.Count = 0 Then
                RemoveLoggingTab()
                tabpages.Remove(PluginsTab)
            End If
            DefaultStatusText()
        End Sub
        Friend Shared ReadOnly Property UserName() As String
            Get
                Return mUserName
            End Get
        End Property
        Friend Shared ReadOnly Property LoggedIn() As Boolean
            Get
                Return mLoggedIn
            End Get
        End Property

        ' User interface management:
        Private Property ShowHideTabs() As Boolean
            Get
                Return MenuShowHide.Checked
            End Get
            Set(ByVal Show As Boolean)
                With tabpages
                    If Show Then
                        .Remove(PluginsTab)
                        .Remove(LoggingTab)
                        .Add(AWBForm.OptionsTab)
                        .Add(AWBForm.MoreOptionsTab)
                        .Add(AWBForm.DabTab)
                        .Add(AWBForm.StartTab)
                        For Each tabp As TabPage In SettingsTabs
                            .Add(tabp)
                        Next
                        If ActivePlugins.Count > 0 Then
                            .Add(LoggingTab)
                            .Add(PluginsTab)
                        End If
                    Else
                        .Remove(AWBForm.OptionsTab)
                        .Remove(AWBForm.MoreOptionsTab)
                        .Remove(AWBForm.DabTab)
                        .Remove(AWBForm.StartTab)
                        .Remove(LoggingTab)
                        For Each tabp As TabPage In SettingsTabs
                            .Remove(tabp)
                        Next
                    End If
                End With
                MenuShowHide.Checked = Show
            End Set
        End Property
        Private Sub DefaultStatusText()
            Select Case ActivePlugins.Count
                Case 0
                    StatusText.Text = "Plugin manager ready"
                Case 1
                    StatusText.Text = "Plugin ready"
                Case Else
                    StatusText.Text = ActivePlugins.Count.ToString("0 plugins ready")
            End Select
            If PluginSettings.ManuallyAssess Then StatusText.Text += " (manual assessments plugin active)"
        End Sub
        Friend Sub AddItemToTextBoxInsertionContextMenu(ByVal ToolStripItems As ToolStripItemCollection)
            DirectCast(AWBTextBox.ContextMenuStrip.Items(conTextBoxInsertionMenuName), _
               ToolStripMenuItem).DropDownItems.AddRange(ToolStripItems)
        End Sub
        Friend Shared Sub AddItemToTextBoxInsertionContextMenu(ByVal ToolStripItem As ToolStripItem, _
        ByVal txt As TextBox)
            DirectCast(txt.ContextMenuStrip.Items(conTextBoxInsertionMenuName), _
               ToolStripMenuItem).DropDownItems.Add(ToolStripItem)
        End Sub
        Friend Shared Sub RemoveItemFromTextBoxInsertionContextMenu(ByVal ToolStripItem As ToolStripItem, _
        ByVal txt As TextBox)
            DirectCast(txt.ContextMenuStrip.Items(conTextBoxInsertionMenuName), _
               ToolStripMenuItem).DropDownItems.Remove(ToolStripItem)
        End Sub

        ' Event handlers - AWB:
        Private Sub AWBClosingEventHandler(ByVal sender As System.Object, ByVal e As FormClosingEventArgs)
            With PluginSettingsControl.MyTrace()
                .WriteBulletedLine("Application closing.", True, False, True)
                .Flush()
                .Close()
            End With
        End Sub
        Private Sub AWBBotModeCheckboxCheckedChangeHandler(ByVal sender As System.Object, _
        ByVal e As System.EventArgs) Handles BotModeCheckbox.CheckedChanged
            Dim Line As String = "Bot-mode "

            If BotMode Then
                Line += "enabled"
                DirectCast(AWBForm.OptionsTab.Controls("groupBox8").Controls("chkSkipNoChanges"), CheckBox).Checked = False

                If NudgeMenuItem.Checked Then TurnNudgesOn()
            Else
                TurnNudgesOff()
                Line += "disabled"
            End If

            NudgeMenuItem.Enabled = BotMode

            PluginSettingsControl.MyTrace.WriteBulletedLine(Line, True, True, True)

            For Each p As PluginBase In ActivePlugins
                p.BotModeChanged(BotMode)
            Next
        End Sub
        Private Sub AWBBotModeCheckboxEnabledChangedHandler(ByVal sender As Object, _
        ByVal e As EventArgs) Handles BotModeCheckbox.EnabledChanged
            If AWBForm.BotModeCheckbox.Enabled AndAlso PluginSettings.ManuallyAssess Then
                AWBForm.BotModeCheckbox.Checked = False
                AWBForm.BotModeCheckbox.Enabled = False
            End If
        End Sub
        Private Sub webcontrol_BusyChanged() Handles webcontrol.BusyChanged
            If webcontrol.Busy Then
                If ActivePlugins.Count > 0 Then PluginSettings.AWBProcessingStart(webcontrol)
                blnWeStoppedAWB = False
            Else
                DefaultStatusText()
                PluginSettingsControl.MyTrace.WriteBulletedLine("AWB stopped processing", True, True, True)
                ' If AWB has stopped and the list is empty we assume the job is finished, so close the log and upload:
                If listmaker.Count = 0 Then PluginSettingsControl.MyTrace.Close()
            End If
        End Sub
        Private Sub StopButtonClickEventHandler(ByVal sender As Object, ByVal e As EventArgs)
            blnWeStoppedAWB = True

            '' HACK because webcontrol.busy doesn't seem to always work:
            DefaultStatusText()
            If Not AssessmentsObject Is Nothing Then AssessmentsObject.Reset()
        End Sub
        Private Sub webcontrol_Loaded()
            ' TODO: This is an overkill measure in the absence of a "Got Username" event from AWB
            'Alternative: If webcontrol.UserName() <> "" Then 'If "" returned, no one logged in.
            If webcontrol.GetLogInStatus Then
                mLoggedIn = True
                mUserName = webcontrol.UserName
                RemoveHandler webcontrol.Loaded, AddressOf webcontrol_Loaded
                RemoveHandler webcontrol.Diffed, AddressOf webcontrol_Loaded
                RemoveHandler webcontrol.None, AddressOf webcontrol_Loaded
            End If
        End Sub

        ' Event handlers - our objects:
        Private Sub OurButtonsClickEventHander(ByVal sender As Object, ByVal e As EventArgs)
            Dim btn As Button = DirectCast(sender, Button)
            Select Case btn.Name
                Case "btnStop"
                    RaiseEvent [Stop]()
                Case "btnStart"
                    RaiseEvent Start()
                Case "btnPreview"
                    RaiseEvent Preview()
                Case "btnSave"
                    RaiseEvent Save()
                Case "btnDiff"
                    RaiseEvent Diff()
                Case "btnIgnore"
                    RaiseEvent Skip()
            End Select
        End Sub
        Private Sub MenuShowHide_Click(ByVal sender As Object, ByVal e As System.EventArgs) _
        Handles MenuShowHide.Click
            ShowHideTabs = MenuShowHide.Checked
        End Sub
        Private Shared Sub StatusText_Click(ByVal sender As Object, ByVal e As System.EventArgs) _
        Handles StatusText.Click
            MessageBox.Show("Move along. There's nothing to see here.")
        End Sub
        Private Sub ManuallyAssessCheckBox_CheckChanged(ByVal sender As Object, ByVal e As System.EventArgs)
            If DirectCast(sender, CheckBox).Checked Then
                StatusText.Text = "Initialising assessments plugin"

                If webcontrol.Busy Then RaiseEvent Stop()
                If blnShowManualAssessmentsInstructions Then
                    Dim dialog As New ManualAssessmentsInstructionsDialog

                    blnShowManualAssessmentsInstructions = Not (dialog.ShowDialog = DialogResult.Yes)
                End If

                AssessmentsObject = New Assessments(webcontrol, AWBForm.SaveButton, PluginSettings.btnSave, _
                   AWBForm.SkipButton, PluginSettings.btnIgnore, AWBForm.OptionsTab, AWBForm.EditSummary, _
                   listmaker, PluginSettings, DirectCast(AWBForm.StartTab.Controls("btnPreview"), Button), _
                   PluginSettings.btnPreview)

                DefaultStatusText()
            Else
                AssessmentsObject.Dispose()
                AssessmentsObject = Nothing
            End If
        End Sub
        Private Sub AssessmentsObject_StopAWB() Handles AssessmentsObject.StopAWB
            StopAWB()
        End Sub
        Private Sub AddGenericTemplateMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) _
        Handles AddGenericTemplateMenuItem.Click
            Dim str As String = Microsoft.VisualBasic.Interaction.InputBox( _
               "Enter the name for this generic plugin").Trim

            If Not str = "" Then
                If Plugins.ContainsKey(str) Then
                    MessageBox.Show("A plugin of this name already exists", "Error", _
                       MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                ElseIf CBool(Microsoft.VisualBasic.InStr(str, " ")) Then
                    str = str.Replace(" ", "")
                    CreateNewGenericPlugin(str, "User")
                Else
                    CreateNewGenericPlugin(str, "User")
                End If
            End If
        End Sub
        Private Sub NudgeMenuItem_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
        Handles NudgeMenuItem.CheckedChanged
            ' TODO: Move from menu item to bot tab (with status label) as and when I make that tab
            If BotMode Then
                If NudgeMenuItem.Checked Then
                    TurnNudgesOn()
                Else
                    TurnNudgesOff()
                End If
            End If
        End Sub

        ' Event handler - Timer:
        Private Sub BotTimer_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles BotTimer.Tick
            Static AWBNudges As Integer

            If Not blnHaveHeardFromAWB AndAlso Not blnWeStoppedAWB AndAlso NudgeMenuItem.Enabled _
            AndAlso NudgeMenuItem.Checked AndAlso listmaker.Count > 0 Then
                For Each p As PluginBase In ActivePlugins
                    If Not p.IAmReady Then
                        PluginSettingsControl.MyTrace.WriteBulletedLine( _
                           "Bot mode: Not nudging AWB as a generic plugin isn't ready yet", True, True, True)
                        Exit Sub
                    End If
                Next
                PluginSettingsControl.MyTrace.WriteBulletedLine( _
                   "Bot mode: Haven't heard from AWB in over 10 minutes, giving it a nudge", True, False, True)
                RaiseEvent [Stop]()
                AWBNudges += 1
                PluginSettings.lblAWBNudges.Text = "Nudges: " & AWBNudges.ToString
                RaiseEvent Start()
            End If

            blnHaveHeardFromAWB = False
        End Sub

        ' XML:
        Friend Shared Function XMLReadBoolean(ByVal reader As System.Xml.XmlTextReader, ByVal param As String, _
        ByVal ExistingValue As Boolean) As Boolean
            If reader.MoveToAttribute(param) Then Return Boolean.Parse(reader.Value) Else Return ExistingValue
        End Function
        Friend Shared Function XMLReadString(ByVal reader As System.Xml.XmlTextReader, ByVal param As String, _
        ByVal ExistingValue As String) As String
            If reader.MoveToAttribute(param) Then Return reader.Value Else Return ExistingValue
        End Function
        Friend Shared Function XMLReadInteger(ByVal reader As System.Xml.XmlTextReader, ByVal param As String, _
        ByVal ExistingValue As Integer) As Integer
            If reader.MoveToAttribute(param) Then Return Integer.Parse(reader.Value) Else Return ExistingValue
        End Function
        Private Sub ReadXML(ByVal Reader As XmlTextReader)
            PluginSettingsControl.MyTrace.WriteBulletedLine("Reading settings from XML", False, True, True)

            blnShowManualAssessmentsInstructions = PluginManager.XMLReadBoolean(Reader, _
               conShowManualAssessmentsInstructions, _
               blnShowManualAssessmentsInstructions) ' must happen BEFORE get ManualAssessment yes/no

            PluginSettings.ReadXML(Reader)

            Dim Count As Integer = XMLReadInteger(Reader, conGenericTemplatesCount, 0)
            If Count > 0 Then ReadGenericTemplatesFromXML(Count, Reader) ' Must set up generic templates 
            'before reading in per-template properties, so that the new template receives a ReadXML() of its own

            For Each plugin As KeyValuePair(Of String, PluginBase) In Plugins
                plugin.Value.ReadXML(Reader)
            Next

            Dim blnNewVal As Boolean = PluginManager.XMLReadBoolean(Reader, conShowHideTabsParm, ShowHideTabs)
            If Not blnNewVal = ShowHideTabs Then _
               ShowHideTabs = blnNewVal ' Mustn't set if the same or we get extra tabs; must happen AFTER plugins

            NudgeMenuItem.Checked = PluginManager.XMLReadBoolean(Reader, conNudgeParm, NudgeMenuItem.Checked)
        End Sub
        Private Sub WriteXML(ByVal Writer As XmlTextWriter)
            Dim strGenericTemplates As New System.Collections.Specialized.StringCollection, i As Integer

            Writer.WriteAttributeString(conNudgeParm, NudgeMenuItem.Checked.ToString)
            Writer.WriteAttributeString(conShowHideTabsParm, ShowHideTabs.ToString)
            Writer.WriteAttributeString(conShowManualAssessmentsInstructions, _
               blnShowManualAssessmentsInstructions.ToString)
            PluginSettings.WriteXML(Writer)
            For Each plugin As KeyValuePair(Of String, PluginBase) In Plugins
                plugin.Value.WriteXML(Writer)
                If plugin.Value.IAmGeneric Then _
                   strGenericTemplates.Add(DirectCast(plugin.Value, IGenericTemplatePlugin) _
                   .GenericTemplateKey)
            Next

            Writer.WriteAttributeString(conGenericTemplatesCount, strGenericTemplates.Count.ToString)

            For Each str As String In strGenericTemplates
                Writer.WriteAttributeString(conGenericTemplate & i.ToString, str)
                i += 1
            Next
        End Sub
        Private Sub LoadSettingsNewWay(ByVal XMLString As String)
            Dim st As New IO.StringReader(XMLString)
            Dim Reader As XmlTextReader = New XmlTextReader(st)

            While Reader.Read()
                If Reader.NodeType = XmlNodeType.Element Then
                    ReadXML(Reader)
                    Exit While
                End If
            End While
        End Sub
        Private Sub ReadGenericTemplatesFromXML(ByVal Count As Integer, ByVal Reader As XmlTextReader)
            Dim PluginName As String

            For i As Integer = 0 To Count - 1
                PluginName = PluginManager.XMLReadString(Reader, conGenericTemplate & i.ToString, "").Trim
                If Not Plugins.ContainsKey(PluginName) Then CreateNewGenericPlugin(PluginName, "ReadXML()")
            Next
        End Sub
    End Class
End Namespace
