using System;
using System.Windows.Forms;
using SysColor = System.Drawing.Color;
using SysPoint = System.Drawing.Point;
using SysSize = System.Drawing.Size;
using SysFont = System.Drawing.Font;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class MiterFlangeForm : Form
    {
        public double FlangeHeight { get; private set; }
        public double FlangeAngle { get; private set; }
        public double BendRadius { get; private set; }
        public double MiterGap { get; private set; }
        public BendPositionEnum BendPosition
        { get; private set; }
        public CornerReliefShapeEnum CornerRelief
        { get; private set; }
        public PartFeatureExtentDirectionEnum
            ExtentDirection
        { get; private set; }
        public HeightDatumTypeEnum HeightDatum
        { get; private set; }

        private NumericUpDown _nudHeight;
        private NumericUpDown _nudAngle;
        private NumericUpDown _nudBendRadius;
        private NumericUpDown _nudMiterGap;
        private ComboBox _cboBendPosition;
        private ComboBox _cboCornerRelief;
        private ComboBox _cboDirection;
        private ComboBox _cboHeightDatum;
        private Button _btnOk;
        private Button _btnCancel;

        // Labels
        private Label _lblHeight;
        private Label _lblAngle;
        private Label _lblBendRadius;
        private Label _lblMiterGap;
        private Label _lblBendPosition;
        private Label _lblCornerRelief;
        private Label _lblDirection;
        private Label _lblHeightDatum;

        public MiterFlangeForm()
        {
            BuildUI();
            LoadDefaults();
            this.Resize += (s, e) => UpdateLayout();
        }

        private void BuildUI()
        {
            this.Text = "Miter Flange Options";
            this.Size = new SysSize(340, 450);
            this.MinimumSize = new SysSize(280, 380);
            this.FormBorderStyle =
                FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition =
                FormStartPosition.CenterScreen;
            this.BackColor =
                SysColor.FromArgb(45, 45, 48);
            this.ForeColor = SysColor.White;
            this.Font = new SysFont("Segoe UI", 9f);

            // ── Labels ────────────────────────────────
            _lblHeight = MakeLabel("Height (in):");
            _lblAngle = MakeLabel("Angle (deg):");
            _lblBendRadius =
                MakeLabel("Bend Radius (in):");
            _lblMiterGap = MakeLabel("Miter Gap (in):");
            _lblBendPosition =
                MakeLabel("Bend Position:");
            _lblCornerRelief =
                MakeLabel("Corner Relief:");
            _lblDirection = MakeLabel("Direction:");
            _lblHeightDatum =
                MakeLabel("Height Datum:");

            // ── Numerics ──────────────────────────────
            _nudHeight = MakeNumeric(
                0.001m, 24m, 0.5m, 3);
            _nudAngle = MakeNumeric(
                1m, 180m, 90m, 1);
            _nudBendRadius = MakeNumeric(
                0.001m, 2m, 0.06m, 3);
            _nudMiterGap = MakeNumeric(
                0m, 1m, 0.02m, 3);

            // ── Combos ────────────────────────────────
            _cboBendPosition = MakeCombo();
            _cboBendPosition.Items.AddRange(
                new object[]
                {
                    "Inside Bend Face",
                    "Outside Base Face",
                    "Adjacent Face",
                    "Tangent To Side Face"
                });

            _cboCornerRelief = MakeCombo();
            _cboCornerRelief.Items.AddRange(
                new object[]
                {
                    "Default",
                    "Round",
                    "Square",
                    "Tear",
                    "Trim To Bend",
                    "Full Round",
                    "Intersection"
                });

            _cboDirection = MakeCombo();
            _cboDirection.Items.AddRange(
                new object[]
                {
                    "Positive",
                    "Negative",
                    "Symmetric"
                });

            _cboHeightDatum = MakeCombo();
            _cboHeightDatum.Items.AddRange(
                new object[]
                {
                    "Inner",
                    "Inner Ortho",
                    "Outer",
                    "Outer Ortho",
                    "Tangent"
                });

            // ── Buttons ───────────────────────────────
            _btnOk = new Button();
            _btnOk.Text = "OK";
            _btnOk.BackColor =
                SysColor.FromArgb(0, 122, 204);
            _btnOk.ForeColor = SysColor.White;
            _btnOk.FlatStyle = FlatStyle.Flat;
            _btnOk.Click += BtnOk_Click;
            this.Controls.Add(_btnOk);

            _btnCancel = new Button();
            _btnCancel.Text = "Cancel";
            _btnCancel.BackColor =
                SysColor.FromArgb(63, 63, 70);
            _btnCancel.ForeColor = SysColor.White;
            _btnCancel.FlatStyle = FlatStyle.Flat;
            _btnCancel.DialogResult =
                DialogResult.Cancel;
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOk;
            this.CancelButton = _btnCancel;

            UpdateLayout();
        }

        private void UpdateLayout()
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            int pad = 12;
            int rowH = 30;
            int btnH = 32;
            int btnAreaH = btnH + pad * 2;

            // Available height for rows
            int availH = h - btnAreaH - pad;
            int rows = 8;
            int spacing = Math.Max(
                rowH, availH / rows);

            // Label width is 45% of form
            int lblW = (int)((w - pad * 3) * 0.45);
            // Control width fills the rest
            int ctlW = w - pad * 3 - lblW;
            int ctlX = pad + lblW + pad;

            int y = pad;

            SetRow(_lblHeight, _nudHeight,
                pad, ctlX, y, lblW, ctlW, rowH);
            y += spacing;

            SetRow(_lblAngle, _nudAngle,
                pad, ctlX, y, lblW, ctlW, rowH);
            y += spacing;

            SetRow(_lblBendRadius, _nudBendRadius,
                pad, ctlX, y, lblW, ctlW, rowH);
            y += spacing;

            SetRow(_lblMiterGap, _nudMiterGap,
                pad, ctlX, y, lblW, ctlW, rowH);
            y += spacing;

            SetRow(_lblBendPosition, _cboBendPosition,
                pad, ctlX, y, lblW, ctlW, rowH);
            y += spacing;

            SetRow(_lblCornerRelief, _cboCornerRelief,
                pad, ctlX, y, lblW, ctlW, rowH);
            y += spacing;

            SetRow(_lblDirection, _cboDirection,
                pad, ctlX, y, lblW, ctlW, rowH);
            y += spacing;

            SetRow(_lblHeightDatum, _cboHeightDatum,
                pad, ctlX, y, lblW, ctlW, rowH);

            // Buttons at bottom
            int btnY = h - btnH - pad;
            int btnW = (w - pad * 3) / 2;

            _btnOk.SetBounds(
                pad, btnY, btnW, btnH);
            _btnCancel.SetBounds(
                pad * 2 + btnW, btnY, btnW, btnH);
        }

        private void SetRow(
            Control lbl, Control ctl,
            int lx, int cx,
            int y, int lw, int cw, int rh)
        {
            lbl.SetBounds(lx, y + 4, lw, 20);
            ctl.SetBounds(cx, y, cw, rh);
        }

        private void LoadDefaults()
        {
            _nudHeight.Value = 0.5m;
            _nudAngle.Value = 90m;
            _nudBendRadius.Value = 0.06m;
            _nudMiterGap.Value = 0.02m;
            _cboBendPosition.SelectedIndex = 0;
            _cboCornerRelief.SelectedIndex = 0;
            _cboDirection.SelectedIndex = 0;
            _cboHeightDatum.SelectedIndex = 0;
        }

        private void BtnOk_Click(
            object sender, EventArgs e)
        {
            FlangeHeight =
                (double)_nudHeight.Value;
            FlangeAngle =
                (double)_nudAngle.Value;
            BendRadius =
                (double)_nudBendRadius.Value;
            MiterGap =
                (double)_nudMiterGap.Value;

            switch (_cboBendPosition.SelectedIndex)
            {
                case 1:
                    BendPosition =
                        BendPositionEnum
                        .kBendPositionOutsideBaseFace;
                    break;
                case 2:
                    BendPosition =
                        BendPositionEnum
                        .kBendPositionAdjacentFace;
                    break;
                case 3:
                    BendPosition =
                        BendPositionEnum
                        .kBendPositionTangentToSideFace;
                    break;
                default:
                    BendPosition =
                        BendPositionEnum
                        .kBendPositionInsideBendFace;
                    break;
            }

            switch (_cboCornerRelief.SelectedIndex)
            {
                case 1:
                    CornerRelief =
                        CornerReliefShapeEnum
                        .kRoundCornerReliefShape;
                    break;
                case 2:
                    CornerRelief =
                        CornerReliefShapeEnum
                        .kSquareCornerReliefShape;
                    break;
                case 3:
                    CornerRelief =
                        CornerReliefShapeEnum
                        .kTearCornerReliefShape;
                    break;
                case 4:
                    CornerRelief =
                        CornerReliefShapeEnum
                        .kTrimToBendReliefShape;
                    break;
                case 5:
                    CornerRelief =
                        CornerReliefShapeEnum
                        .kFullRoundCornerReliefShape;
                    break;
                case 6:
                    CornerRelief =
                        CornerReliefShapeEnum
                        .kIntersectionCornerReliefShape;
                    break;
                default:
                    CornerRelief =
                        CornerReliefShapeEnum
                        .kDefaultCornerReliefShape;
                    break;
            }

            switch (_cboDirection.SelectedIndex)
            {
                case 1:
                    ExtentDirection =
                        PartFeatureExtentDirectionEnum
                        .kNegativeExtentDirection;
                    break;
                case 2:
                    ExtentDirection =
                        PartFeatureExtentDirectionEnum
                        .kSymmetricExtentDirection;
                    break;
                default:
                    ExtentDirection =
                        PartFeatureExtentDirectionEnum
                        .kPositiveExtentDirection;
                    break;
            }

            switch (_cboHeightDatum.SelectedIndex)
            {
                case 1:
                    HeightDatum =
                        HeightDatumTypeEnum
                        .kHeightDatumInnerOrtho;
                    break;
                case 2:
                    HeightDatum =
                        HeightDatumTypeEnum
                        .kHeightDatumOuter;
                    break;
                case 3:
                    HeightDatum =
                        HeightDatumTypeEnum
                        .kHeightDatumOuterOrtho;
                    break;
                case 4:
                    HeightDatum =
                        HeightDatumTypeEnum
                        .kHeightDatumTangent;
                    break;
                default:
                    HeightDatum =
                        HeightDatumTypeEnum
                        .kHeightDatumInner;
                    break;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private Label MakeLabel(string text)
        {
            var lbl = new Label();
            lbl.Text = text;
            lbl.ForeColor = SysColor.White;
            lbl.AutoSize = false;
            this.Controls.Add(lbl);
            return lbl;
        }

        private NumericUpDown MakeNumeric(
            decimal min, decimal max,
            decimal val, int decimals)
        {
            var nud = new NumericUpDown();
            nud.Minimum = min;
            nud.Maximum = max;
            nud.Value = val;
            nud.DecimalPlaces = decimals;
            nud.Increment = 0.001m;
            nud.BackColor =
                SysColor.FromArgb(37, 37, 38);
            nud.ForeColor = SysColor.White;
            this.Controls.Add(nud);
            return nud;
        }

        private ComboBox MakeCombo()
        {
            var cbo = new ComboBox();
            cbo.DropDownStyle =
                ComboBoxStyle.DropDownList;
            cbo.BackColor =
                SysColor.FromArgb(37, 37, 38);
            cbo.ForeColor = SysColor.White;
            cbo.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(cbo);
            return cbo;
        }
    }
}