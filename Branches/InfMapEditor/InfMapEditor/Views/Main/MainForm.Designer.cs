namespace InfMapEditor.Views.Main
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.statusBar = new System.Windows.Forms.StatusStrip();
            this.statusLabelMousePosition = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuBar = new System.Windows.Forms.MenuStrip();
            this.MenuItemFile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileNew = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSave = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSeperator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemFileImport = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileImportLevel = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileImportMap = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSeperator2 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemEditShowGrid = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemEditChangeDir = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemLayerFloors = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemLayerObjects = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemLayerPhysics = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemLayerVision = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjects = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjDoors = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjFlags = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjHides = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjNested = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjParallax = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjPortals = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjSounds = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjSwitches = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjText = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemObjWarps = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemWindowMainPalette = new System.Windows.Forms.ToolStripMenuItem();
            this.MainFormSplitContainer = new System.Windows.Forms.SplitContainer();
            this.mainFormPanel = new System.Windows.Forms.Panel();
            this.miniMap = new InfMapEditor.Views.Main.Partials.MinimapControl();
            this.mapControl = new InfMapEditor.Views.Main.Partials.MapControl();
            this.toolBar = new System.Windows.Forms.ToolStrip();
            this.toolStripNewButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripOpenButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSaveButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripUndoButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripRedoButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripCutButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripCopyButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripPasteButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripDeleteButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripFindButton = new System.Windows.Forms.ToolStripButton();
            this.statusBar.SuspendLayout();
            this.menuBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainFormSplitContainer)).BeginInit();
            this.MainFormSplitContainer.Panel1.SuspendLayout();
            this.MainFormSplitContainer.Panel2.SuspendLayout();
            this.MainFormSplitContainer.SuspendLayout();
            this.toolBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusBar
            // 
            this.statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabelMousePosition});
            this.statusBar.Location = new System.Drawing.Point(0, 606);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(997, 24);
            this.statusBar.TabIndex = 0;
            this.statusBar.Text = "statusStrip1";
            // 
            // statusLabelMousePosition
            // 
            this.statusLabelMousePosition.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.statusLabelMousePosition.Name = "statusLabelMousePosition";
            this.statusLabelMousePosition.Size = new System.Drawing.Size(86, 19);
            this.statusLabelMousePosition.Text = "Map Pos (0, 0)";
            // 
            // menuBar
            // 
            this.menuBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemFile,
            this.MenuItemEdit,
            this.MenuItemLayers,
            this.MenuItemObjects,
            this.MenuItemWindow});
            this.menuBar.Location = new System.Drawing.Point(0, 0);
            this.menuBar.Name = "menuBar";
            this.menuBar.Size = new System.Drawing.Size(997, 24);
            this.menuBar.TabIndex = 1;
            this.menuBar.Text = "menuStrip1";
            // 
            // MenuItemFile
            // 
            this.MenuItemFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemFileNew,
            this.MenuItemFileOpen,
            this.MenuItemFileSave,
            this.MenuItemFileSaveAs,
            this.MenuItemFileSeperator1,
            this.MenuItemFileImport,
            this.MenuItemFileSeperator2,
            this.MenuItemFileExit});
            this.MenuItemFile.Name = "MenuItemFile";
            this.MenuItemFile.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.MenuItemFile.Size = new System.Drawing.Size(37, 20);
            this.MenuItemFile.Text = "File";
            // 
            // MenuItemFileNew
            // 
            this.MenuItemFileNew.Name = "MenuItemFileNew";
            this.MenuItemFileNew.Size = new System.Drawing.Size(114, 22);
            this.MenuItemFileNew.Text = "New";
            this.MenuItemFileNew.Click += new System.EventHandler(this.MenuItemFileNew_Click);
            // 
            // MenuItemFileOpen
            // 
            this.MenuItemFileOpen.Name = "MenuItemFileOpen";
            this.MenuItemFileOpen.Size = new System.Drawing.Size(114, 22);
            this.MenuItemFileOpen.Text = "Open";
            this.MenuItemFileOpen.Click += new System.EventHandler(this.MenuItemFileOpen_Click);
            // 
            // MenuItemFileSave
            // 
            this.MenuItemFileSave.Name = "MenuItemFileSave";
            this.MenuItemFileSave.Size = new System.Drawing.Size(114, 22);
            this.MenuItemFileSave.Text = "Save";
            this.MenuItemFileSave.Click += new System.EventHandler(this.MenuItemFileSave_Click);
            // 
            // MenuItemFileSaveAs
            // 
            this.MenuItemFileSaveAs.Name = "MenuItemFileSaveAs";
            this.MenuItemFileSaveAs.Size = new System.Drawing.Size(114, 22);
            this.MenuItemFileSaveAs.Text = "Save As";
            this.MenuItemFileSaveAs.Click += new System.EventHandler(this.MenuItemFileSaveAs_Click);
            // 
            // MenuItemFileSeperator1
            // 
            this.MenuItemFileSeperator1.Name = "MenuItemFileSeperator1";
            this.MenuItemFileSeperator1.Size = new System.Drawing.Size(111, 6);
            // 
            // MenuItemFileImport
            // 
            this.MenuItemFileImport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemFileImportLevel,
            this.MenuItemFileImportMap});
            this.MenuItemFileImport.Name = "MenuItemFileImport";
            this.MenuItemFileImport.Size = new System.Drawing.Size(114, 22);
            this.MenuItemFileImport.Text = "Import";
            // 
            // MenuItemFileImportLevel
            // 
            this.MenuItemFileImportLevel.Name = "MenuItemFileImportLevel";
            this.MenuItemFileImportLevel.Size = new System.Drawing.Size(141, 22);
            this.MenuItemFileImportLevel.Text = "Level (*.lvl)";
            this.MenuItemFileImportLevel.Click += new System.EventHandler(this.MenuItemFileImportLevel_Click);
            // 
            // MenuItemFileImportMap
            // 
            this.MenuItemFileImportMap.Name = "MenuItemFileImportMap";
            this.MenuItemFileImportMap.Size = new System.Drawing.Size(141, 22);
            this.MenuItemFileImportMap.Text = "Map (*.map)";
            this.MenuItemFileImportMap.Click += new System.EventHandler(this.MenuItemFileImportMap_Click);
            // 
            // MenuItemFileSeperator2
            // 
            this.MenuItemFileSeperator2.Name = "MenuItemFileSeperator2";
            this.MenuItemFileSeperator2.Size = new System.Drawing.Size(111, 6);
            // 
            // MenuItemFileExit
            // 
            this.MenuItemFileExit.Name = "MenuItemFileExit";
            this.MenuItemFileExit.Size = new System.Drawing.Size(114, 22);
            this.MenuItemFileExit.Text = "Exit";
            this.MenuItemFileExit.Click += new System.EventHandler(this.MenuItemFileExit_Click);
            // 
            // MenuItemEdit
            // 
            this.MenuItemEdit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemEditShowGrid,
            this.MenuItemEditChangeDir});
            this.MenuItemEdit.Name = "MenuItemEdit";
            this.MenuItemEdit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.MenuItemEdit.Size = new System.Drawing.Size(39, 20);
            this.MenuItemEdit.Text = "Edit";
            // 
            // MenuItemEditShowGrid
            // 
            this.MenuItemEditShowGrid.Name = "MenuItemEditShowGrid";
            this.MenuItemEditShowGrid.Size = new System.Drawing.Size(190, 22);
            this.MenuItemEditShowGrid.Text = "Grid...";
            this.MenuItemEditShowGrid.Click += new System.EventHandler(this.MenuItemEditShowGrid_Click);
            // 
            // MenuItemEditChangeDir
            // 
            this.MenuItemEditChangeDir.Name = "MenuItemEditChangeDir";
            this.MenuItemEditChangeDir.Size = new System.Drawing.Size(190, 22);
            this.MenuItemEditChangeDir.Text = "Change Working Dir...";
            this.MenuItemEditChangeDir.Click += new System.EventHandler(this.MenuItemEditWorkingDir_Click);
            // 
            // MenuItemLayers
            // 
            this.MenuItemLayers.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemLayerFloors,
            this.MenuItemLayerObjects,
            this.MenuItemLayerPhysics,
            this.MenuItemLayerVision});
            this.MenuItemLayers.Name = "MenuItemLayers";
            this.MenuItemLayers.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
            this.MenuItemLayers.Size = new System.Drawing.Size(52, 20);
            this.MenuItemLayers.Text = "Layers";
            // 
            // MenuItemLayerFloors
            // 
            this.MenuItemLayerFloors.Checked = true;
            this.MenuItemLayerFloors.CheckOnClick = true;
            this.MenuItemLayerFloors.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MenuItemLayerFloors.Name = "MenuItemLayerFloors";
            this.MenuItemLayerFloors.Size = new System.Drawing.Size(114, 22);
            this.MenuItemLayerFloors.Text = "Floors";
            this.MenuItemLayerFloors.ToolTipText = "When enabled, shows the floor tiles within the map.";
            this.MenuItemLayerFloors.Click += new System.EventHandler(this.MenuItemObjFloors_Click);
            // 
            // MenuItemLayerObjects
            // 
            this.MenuItemLayerObjects.Checked = true;
            this.MenuItemLayerObjects.CheckOnClick = true;
            this.MenuItemLayerObjects.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MenuItemLayerObjects.Name = "MenuItemLayerObjects";
            this.MenuItemLayerObjects.Size = new System.Drawing.Size(114, 22);
            this.MenuItemLayerObjects.Text = "Objects";
            this.MenuItemLayerObjects.ToolTipText = "When enabled, shows the objects in the map.";
            this.MenuItemLayerObjects.Click += new System.EventHandler(this.MenuItemObjObjects_Click);
            // 
            // MenuItemLayerPhysics
            // 
            this.MenuItemLayerPhysics.Checked = true;
            this.MenuItemLayerPhysics.CheckOnClick = true;
            this.MenuItemLayerPhysics.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MenuItemLayerPhysics.Name = "MenuItemLayerPhysics";
            this.MenuItemLayerPhysics.Size = new System.Drawing.Size(114, 22);
            this.MenuItemLayerPhysics.Text = "Physics";
            this.MenuItemLayerPhysics.ToolTipText = "When enabled, shows the physics in the map.";
            this.MenuItemLayerPhysics.Click += new System.EventHandler(this.MenuItemObjPhysics_Click);
            // 
            // MenuItemLayerVision
            // 
            this.MenuItemLayerVision.Checked = true;
            this.MenuItemLayerVision.CheckOnClick = true;
            this.MenuItemLayerVision.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MenuItemLayerVision.Name = "MenuItemLayerVision";
            this.MenuItemLayerVision.Size = new System.Drawing.Size(114, 22);
            this.MenuItemLayerVision.Text = "Vision";
            this.MenuItemLayerVision.ToolTipText = "When enabled, shows the vision numbers in the map.";
            this.MenuItemLayerVision.Click += new System.EventHandler(this.MenuItemObjVision_Click);
            // 
            // MenuItemObjects
            // 
            this.MenuItemObjects.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemObjDoors,
            this.MenuItemObjFlags,
            this.MenuItemObjHides,
            this.MenuItemObjNested,
            this.MenuItemObjParallax,
            this.MenuItemObjPortals,
            this.MenuItemObjSounds,
            this.MenuItemObjSwitches,
            this.MenuItemObjText,
            this.MenuItemObjWarps});
            this.MenuItemObjects.Name = "MenuItemObjects";
            this.MenuItemObjects.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.MenuItemObjects.Size = new System.Drawing.Size(59, 20);
            this.MenuItemObjects.Text = "Objects";
            // 
            // MenuItemObjDoors
            // 
            this.MenuItemObjDoors.Name = "MenuItemObjDoors";
            this.MenuItemObjDoors.Size = new System.Drawing.Size(129, 22);
            this.MenuItemObjDoors.Text = "Doors...";
            // 
            // MenuItemObjFlags
            // 
            this.MenuItemObjFlags.Name = "MenuItemObjFlags";
            this.MenuItemObjFlags.Size = new System.Drawing.Size(129, 22);
            this.MenuItemObjFlags.Text = "Flags...";
            // 
            // MenuItemObjHides
            // 
            this.MenuItemObjHides.Name = "MenuItemObjHides";
            this.MenuItemObjHides.Size = new System.Drawing.Size(129, 22);
            this.MenuItemObjHides.Text = "Hides...";
            // 
            // MenuItemObjNested
            // 
            this.MenuItemObjNested.Name = "MenuItemObjNested";
            this.MenuItemObjNested.Size = new System.Drawing.Size(129, 22);
            this.MenuItemObjNested.Text = "Nested...";
            // 
            // MenuItemObjParallax
            // 
            this.MenuItemObjParallax.Name = "MenuItemObjParallax";
            this.MenuItemObjParallax.Size = new System.Drawing.Size(129, 22);
            this.MenuItemObjParallax.Text = "Parallax...";
            // 
            // MenuItemObjPortals
            // 
            this.MenuItemObjPortals.Name = "MenuItemObjPortals";
            this.MenuItemObjPortals.Size = new System.Drawing.Size(129, 22);
            this.MenuItemObjPortals.Text = "Portals...";
            // 
            // MenuItemObjSounds
            // 
            this.MenuItemObjSounds.Name = "MenuItemObjSounds";
            this.MenuItemObjSounds.Size = new System.Drawing.Size(129, 22);
            this.MenuItemObjSounds.Text = "Sounds...";
            // 
            // MenuItemObjSwitches
            // 
            this.MenuItemObjSwitches.Name = "MenuItemObjSwitches";
            this.MenuItemObjSwitches.Size = new System.Drawing.Size(129, 22);
            this.MenuItemObjSwitches.Text = "Switches...";
            // 
            // MenuItemObjText
            // 
            this.MenuItemObjText.Name = "MenuItemObjText";
            this.MenuItemObjText.Size = new System.Drawing.Size(129, 22);
            this.MenuItemObjText.Text = "Text...";
            // 
            // MenuItemObjWarps
            // 
            this.MenuItemObjWarps.Name = "MenuItemObjWarps";
            this.MenuItemObjWarps.Size = new System.Drawing.Size(129, 22);
            this.MenuItemObjWarps.Text = "Warps...";
            // 
            // MenuItemWindow
            // 
            this.MenuItemWindow.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemWindowMainPalette});
            this.MenuItemWindow.Name = "MenuItemWindow";
            this.MenuItemWindow.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
            this.MenuItemWindow.Size = new System.Drawing.Size(63, 20);
            this.MenuItemWindow.Text = "Window";
            // 
            // MenuItemWindowMainPalette
            // 
            this.MenuItemWindowMainPalette.Name = "MenuItemWindowMainPalette";
            this.MenuItemWindowMainPalette.Size = new System.Drawing.Size(140, 22);
            this.MenuItemWindowMainPalette.Text = "Main Palette";
            this.MenuItemWindowMainPalette.ToolTipText = "Opens or undocks the Palette Window";
            this.MenuItemWindowMainPalette.Click += new System.EventHandler(this.MenuItemWindowMainPalette_Click);
            // 
            // MainFormSplitContainer
            // 
            this.MainFormSplitContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.MainFormSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainFormSplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.MainFormSplitContainer.IsSplitterFixed = true;
            this.MainFormSplitContainer.Location = new System.Drawing.Point(0, 49);
            this.MainFormSplitContainer.Name = "MainFormSplitContainer";
            // 
            // MainFormSplitContainer.Panel1
            // 
            this.MainFormSplitContainer.Panel1.Controls.Add(this.mainFormPanel);
            this.MainFormSplitContainer.Panel1.Controls.Add(this.miniMap);
            // 
            // MainFormSplitContainer.Panel2
            // 
            this.MainFormSplitContainer.Panel2.Controls.Add(this.mapControl);
            this.MainFormSplitContainer.Size = new System.Drawing.Size(997, 557);
            this.MainFormSplitContainer.SplitterDistance = 271;
            this.MainFormSplitContainer.TabIndex = 3;
            // 
            // mainFormPanel
            // 
            this.mainFormPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.mainFormPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.mainFormPanel.Location = new System.Drawing.Point(0, 260);
            this.mainFormPanel.Name = "mainFormPanel";
            this.mainFormPanel.Size = new System.Drawing.Size(267, 293);
            this.mainFormPanel.TabIndex = 1;
            // 
            // miniMap
            // 
            this.miniMap.BackColor = System.Drawing.SystemColors.Control;
            this.miniMap.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.miniMap.Dock = System.Windows.Forms.DockStyle.Top;
            this.miniMap.Image = ((System.Drawing.Bitmap)(resources.GetObject("miniMap.Image")));
            this.miniMap.Location = new System.Drawing.Point(0, 0);
            this.miniMap.Name = "miniMap";
            this.miniMap.Size = new System.Drawing.Size(267, 256);
            this.miniMap.TabIndex = 0;
            // 
            // mapControl
            // 
            this.mapControl.AutoSize = true;
            this.mapControl.BackColor = System.Drawing.SystemColors.Control;
            this.mapControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapControl.Location = new System.Drawing.Point(0, 0);
            this.mapControl.Name = "mapControl";
            this.mapControl.Size = new System.Drawing.Size(718, 553);
            this.mapControl.TabIndex = 3;
            // 
            // toolBar
            // 
            this.toolBar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripNewButton,
            this.toolStripOpenButton,
            this.toolStripSaveButton,
            this.toolStripSeparator1,
            this.toolStripUndoButton,
            this.toolStripRedoButton,
            this.toolStripSeparator2,
            this.toolStripCutButton,
            this.toolStripCopyButton,
            this.toolStripPasteButton,
            this.toolStripDeleteButton,
            this.toolStripSeparator3,
            this.toolStripFindButton});
            this.toolBar.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.toolBar.Location = new System.Drawing.Point(0, 24);
            this.toolBar.Name = "toolBar";
            this.toolBar.Size = new System.Drawing.Size(997, 25);
            this.toolBar.TabIndex = 2;
            // 
            // toolStripNewButton
            // 
            this.toolStripNewButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolStripNewButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripNewButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripNewButton.Image")));
            this.toolStripNewButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripNewButton.Name = "toolStripNewButton";
            this.toolStripNewButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripNewButton.ToolTipText = "New";
            this.toolStripNewButton.Click += new System.EventHandler(this.MenuItemFileNew_Click);
            // 
            // toolStripOpenButton
            // 
            this.toolStripOpenButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolStripOpenButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripOpenButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripOpenButton.Image")));
            this.toolStripOpenButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripOpenButton.Name = "toolStripOpenButton";
            this.toolStripOpenButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripOpenButton.ToolTipText = "Open";
            this.toolStripOpenButton.Click += new System.EventHandler(this.MenuItemFileOpen_Click);
            // 
            // toolStripSaveButton
            // 
            this.toolStripSaveButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolStripSaveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripSaveButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSaveButton.Image")));
            this.toolStripSaveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripSaveButton.Name = "toolStripSaveButton";
            this.toolStripSaveButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripSaveButton.ToolTipText = "Save";
            this.toolStripSaveButton.Click += new System.EventHandler(this.MenuItemFileSave_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripUndoButton
            // 
            this.toolStripUndoButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolStripUndoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripUndoButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripUndoButton.Image")));
            this.toolStripUndoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripUndoButton.Name = "toolStripUndoButton";
            this.toolStripUndoButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripUndoButton.ToolTipText = "Undo";
            // 
            // toolStripRedoButton
            // 
            this.toolStripRedoButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolStripRedoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripRedoButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripRedoButton.Image")));
            this.toolStripRedoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripRedoButton.Name = "toolStripRedoButton";
            this.toolStripRedoButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripRedoButton.ToolTipText = "Redo";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripCutButton
            // 
            this.toolStripCutButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toolStripCutButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripCutButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripCutButton.Image")));
            this.toolStripCutButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripCutButton.Name = "toolStripCutButton";
            this.toolStripCutButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripCutButton.ToolTipText = "Cut";
            // 
            // toolStripCopyButton
            // 
            this.toolStripCopyButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripCopyButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripCopyButton.Image")));
            this.toolStripCopyButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripCopyButton.Name = "toolStripCopyButton";
            this.toolStripCopyButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripCopyButton.ToolTipText = "Copy";
            // 
            // toolStripPasteButton
            // 
            this.toolStripPasteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripPasteButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripPasteButton.Image")));
            this.toolStripPasteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripPasteButton.Name = "toolStripPasteButton";
            this.toolStripPasteButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripPasteButton.ToolTipText = "Paste";
            // 
            // toolStripDeleteButton
            // 
            this.toolStripDeleteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDeleteButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDeleteButton.Image")));
            this.toolStripDeleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDeleteButton.Name = "toolStripDeleteButton";
            this.toolStripDeleteButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripDeleteButton.ToolTipText = "Delete";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripFindButton
            // 
            this.toolStripFindButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripFindButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripFindButton.Image")));
            this.toolStripFindButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripFindButton.Name = "toolStripFindButton";
            this.toolStripFindButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripFindButton.ToolTipText = "Find";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(997, 630);
            this.Controls.Add(this.MainFormSplitContainer);
            this.Controls.Add(this.toolBar);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.menuBar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuBar;
            this.Name = "MainForm";
            this.Text = "Infantry Map Editor";
            this.statusBar.ResumeLayout(false);
            this.statusBar.PerformLayout();
            this.menuBar.ResumeLayout(false);
            this.menuBar.PerformLayout();
            this.MainFormSplitContainer.Panel1.ResumeLayout(false);
            this.MainFormSplitContainer.Panel2.ResumeLayout(false);
            this.MainFormSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainFormSplitContainer)).EndInit();
            this.MainFormSplitContainer.ResumeLayout(false);
            this.toolBar.ResumeLayout(false);
            this.toolBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusBar;
        private System.Windows.Forms.MenuStrip menuBar;
        private System.Windows.Forms.SplitContainer MainFormSplitContainer;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFile;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjects;
        private System.Windows.Forms.ToolStripMenuItem MenuItemEdit;
        private System.Windows.Forms.ToolStripMenuItem MenuItemWindow;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjDoors;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjFlags;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjHides;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjNested;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjParallax;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjPortals;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjSounds;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjSwitches;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjText;
        private System.Windows.Forms.ToolStripMenuItem MenuItemObjWarps;
        private Partials.MapControl mapControl;
        private System.Windows.Forms.ToolStripStatusLabel statusLabelMousePosition;
        private System.Windows.Forms.ToolStripMenuItem MenuItemEditShowGrid;
        private System.Windows.Forms.ToolStripMenuItem MenuItemLayers;
        private System.Windows.Forms.ToolStripMenuItem MenuItemLayerFloors;
        private System.Windows.Forms.ToolStripMenuItem MenuItemLayerObjects;
        private System.Windows.Forms.ToolStripMenuItem MenuItemLayerPhysics;
        private System.Windows.Forms.ToolStripMenuItem MenuItemLayerVision;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileImport;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileImportLevel;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileImportMap;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileNew;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileOpen;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileSave;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileSaveAs;
        private System.Windows.Forms.ToolStripSeparator MenuItemFileSeperator1;
        private System.Windows.Forms.ToolStripSeparator MenuItemFileSeperator2;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileExit;
        private System.Windows.Forms.ToolStripMenuItem MenuItemEditChangeDir;
        private System.Windows.Forms.ToolStrip toolBar;
        private System.Windows.Forms.ToolStripButton toolStripNewButton;
        private System.Windows.Forms.ToolStripButton toolStripOpenButton;
        private System.Windows.Forms.ToolStripButton toolStripSaveButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripUndoButton;
        private System.Windows.Forms.ToolStripButton toolStripRedoButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripCutButton;
        private System.Windows.Forms.ToolStripButton toolStripCopyButton;
        private System.Windows.Forms.ToolStripButton toolStripPasteButton;
        private System.Windows.Forms.ToolStripButton toolStripDeleteButton;
        private System.Windows.Forms.ToolStripButton toolStripFindButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem MenuItemWindowMainPalette;
        private Partials.MinimapControl miniMap;
        private System.Windows.Forms.Panel mainFormPanel;
    }
}

