﻿using DTC.New.Presets.V2.Base;
using DTC.Utilities;

namespace DTC.New.UI.Base.Pages;

public partial class PresetsPage : Page
{
    private readonly Aircraft _aircraft;

    public override string PageTitle
    {
        get
        {
            return _aircraft.Name + " Presets";
        }
    }

    public PresetsPage(Aircraft aircraft)
    {
        InitializeComponent();
        _aircraft = aircraft;
        _aircraft.PresetsChanged += OnPresetsChanged;
        dgPresets.RefreshList(_aircraft.Presets);
        RefreshButtons();
    }

    private void OnPresetsChanged(object sender, EventArgs e)
    {
        dgPresets.RefreshList(_aircraft.Presets);
        RefreshButtons();
    }

    private void dgPresets_SelectionChanged(object sender, System.EventArgs e)
    {
        RefreshButtons();
    }

    private void btnAdd_Click(object sender, System.EventArgs e)
    {
        AddRenamePreset(null);
    }

    private void btnRename_Click(object sender, System.EventArgs e)
    {
        if (dgPresets.SelectedRows.Count > 0)
        {
            AddRenamePreset((Preset)dgPresets.SelectedRows[0].DataBoundItem);
        }
    }

    private void btnEdit_Click(object sender, System.EventArgs e)
    {
        if (dgPresets.SelectedRows.Count > 0)
        {
            ShowPreset((Preset)dgPresets.SelectedRows[0].DataBoundItem);
        }
    }

    public void ShowPreset(Preset preset)
    {
        var acPage = AircraftPageFactory.Make(_aircraft, preset);
        MainForm.AddPage(acPage);
    }

    public bool ShowPreset(string name)
    {
        foreach (var p in _aircraft.Presets)
        {
            if (p.Name == name)
            {
                ShowPreset((Preset)p);
                return true;
            }
        }

        return false;
    }

    private void RefreshButtons()
    {
        var selected = dgPresets.SelectedRows.Count > 0;

        btnEdit.Enabled = selected;
        btnRename.Enabled = selected;
        btnClone.Enabled = selected;
        btnDelete.Enabled = selected;
    }

    private void AddRenamePreset(IPreset preset, Action callback = null)
    {
        var dialog = new PresetName();
        this.Controls.Add(dialog);
        dialog.Left = (this.Width / 2) - (dialog.Width / 2);
        dialog.Top = (this.Height / 2) - (dialog.Height / 2);
        dialog.txtName.Focus();
        dialog.BringToFront();

        string oldName = null;

        if (preset != null)
        {
            oldName = preset.Name;
            dialog.txtName.Text = preset.Name;
        }

        pnlContent.Enabled = false;

        dialog.DialogResultCallback = (DialogResult result) =>
        {
            var newName = dialog.txtName.Text;
            if (string.IsNullOrEmpty(newName) && result == DialogResult.OK)
            {
                return;
            }
            if (result == DialogResult.OK)
            {
                if (preset == null)
                {
                    if (IsPresetNotInUse(newName))
                    {
                        var newPreset = _aircraft.CreatePreset(newName);
                        _aircraft.PersistPreset(newPreset);
                        ShowPreset(newPreset);
                    }
                }
                else
                {
                    preset.Name = newName;
                    if (preset.Name != oldName)
                    {
                        if (IsPresetNotInUse(newName))
                        {
                            FileStorage.RenamePresetFile(_aircraft, preset, oldName);
                        }
                    }
                }
            }
            this.Controls.Remove(dialog);
            pnlContent.Enabled = true;
            _aircraft.RefreshPresetList();
            dgPresets.RefreshList(_aircraft.Presets);
            if (callback != null)
            {
                callback();
            }
        };
    }

    private bool IsPresetNotInUse(string name)
    {
        if (FileStorage.PresetExists(_aircraft, name))
        {
            return DTCMessageBox.ShowQuestion($"Preset with name {name} already exists. Do you want to overwrite it?");
        }
        return true;
    }

    private void dgPresets_DoubleClick(object sender, System.EventArgs e)
    {
        if (dgPresets.SelectedRows.Count > 0)
        {
            ShowPreset((Preset)dgPresets.SelectedRows[0].DataBoundItem);
        }
    }

    private void btnClone_Click(object sender, System.EventArgs e)
    {
        if (dgPresets.SelectedRows.Count > 0)
        {
            var preset = (Preset)dgPresets.SelectedRows[0].DataBoundItem;
            var cloned = _aircraft.ClonePreset(preset);
            if (cloned != null)
            {
                AddRenamePreset(cloned);
            }
        }
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        if (dgPresets.SelectedRows.Count > 0)
        {
            var preset = (Preset)dgPresets.SelectedRows[0].DataBoundItem;

            if (DTCMessageBox.ShowQuestion("Do you really want to delete " + preset.Name + "?"))
            {
                _aircraft.DeletePreset(preset);
                dgPresets.RefreshList(_aircraft.Presets);
            }
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_aircraft != null)
        {
            _aircraft.PresetsChanged -= OnPresetsChanged;
        }
        base.OnHandleDestroyed(e);
    }
}
