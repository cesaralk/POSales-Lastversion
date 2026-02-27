using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace POSales
{
    public partial class StockIn : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        string stitle = "Point Of Sales";
        MainForm main;

        // Pricing columns (stored per StockIn row)
        // Pending grid uses these Names. History grid uses *Hist Names to avoid WinForms designer
        // "The name X is already in use by another component" errors.
        private const string ColCostPrice = "CostPrice";
        private const string ColMarkup = "Markup";
        private const string ColSellPrice = "Price";

        private const string ColCostPriceHist = "CostPriceHist";
        private const string ColMarkupHist = "MarkupHist";
        private const string ColSellPriceHist = "PriceHist";

        // Product history tab controls (created at runtime)
        private TabPage _tabProductHistory;
        private TextBox _txtHistoryScan;
        private DataGridView _dgvProductHistory;
        public StockIn(MainForm mn)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            main = mn;
            LoadSupplier();
            GetRefeNo();
            txtStockInBy.Text = main.lblUsername.Text;

            // Ensure pricing columns are visible/editable in Stock In grid and visible in history grid
            EnsurePricingColumnsForGrid(dgvStockIn, editable: true);
            EnsurePricingColumnsForGrid(dgvInStockHistory, editable: false);

            // Prevent WinForms default DataGridView error popup
            dgvStockIn.DataError += Dgv_DataError;
            dgvInStockHistory.DataError += Dgv_DataError;

            // Auto-calc + persist price when user edits Cost/Markup
            dgvStockIn.CellEndEdit += dgvStockIn_CellEndEdit_Pricing;
            dgvStockIn.CellValidating += dgvStockIn_CellValidating_Pricing;

            // Add product stock-in history tab (scan -> show all history for product)
            AddProductHistoryTab();


        }

        public void GetRefeNo()
        {
            Random rnd = new Random();
            txtRefNo.Clear();
            txtRefNo.Text += rnd.Next();

            // Auto-fill Stock In By with the currently logged-in username
            // to avoid retyping it every time a reference number is generated.
            if (!string.IsNullOrWhiteSpace(UserSession.Username))
                txtStockInBy.Text = UserSession.Username;
        }

        public void LoadSupplier()
        {
            cbSupplier.Items.Clear();
            cbSupplier.DataSource = dbcon.getTable("SELECT * FROM tbSupplier");
            cbSupplier.DisplayMember = "supplier";
        }

        public void ProductForSupplier(string pcode)
        {
            string supplier = "";
            cn.Open();
            cm = new SqlCommand("SELECT * FROM vwStockIn WHERE pcode LIKE '" + pcode + "'", cn);
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                supplier = dr["supplier"].ToString();
            }
            dr.Close();
            cn.Close();
            cbSupplier.Text = supplier;
        }

        public void LoadStockIn()
        {
            int i = 0;
            dgvStockIn.Rows.Clear();
            cn.Open();

            // Use base tables so we can always retrieve pricing history
            cm = new SqlCommand(@"
                SELECT
                    s.id,
                    s.refno,
                    s.pcode,
                    p.pdesc,
                    s.qty,
                    s.sdate,
                    s.stockinby,
                    s.status,
                    ISNULL(sup.supplier,'') AS supplier,
                    ISNULL(s.CostPrice, p.costprice) AS CostPrice,
                    ISNULL(s.Markup, p.markup) AS Markup,
                    ISNULL(s.Price, p.price) AS Price
                FROM tbStockIn s
                INNER JOIN tbProduct p ON p.pcode = s.pcode
                LEFT JOIN tbSupplier sup ON sup.id = s.supplierid
                WHERE s.refno = @refno AND s.status = 'Pending'
                ORDER BY s.sdate DESC, s.id DESC",
                cn);
            cm.Parameters.AddWithValue("@refno", txtRefNo.Text);

            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;

                // IMPORTANT: dgvStockIn has an Image column named "Delete".
                // Always set values by column name to avoid casting strings into the Image column.
                int rowIndex = dgvStockIn.Rows.Add();
                var row = dgvStockIn.Rows[rowIndex];
                row.Cells["Column1"].Value = i;
                row.Cells["Column9"].Value = dr["id"].ToString();
                row.Cells["Column10"].Value = dr["refno"].ToString();
                row.Cells["Column2"].Value = dr["pcode"].ToString();
                row.Cells["Column4"].Value = dr["pdesc"].ToString();
                row.Cells["Column5"].Value = dr["qty"].ToString();
                row.Cells["Column6"].Value = dr["sdate"].ToString();
                row.Cells["Column7"].Value = dr["stockinby"].ToString();
                row.Cells["Column8"].Value = dr["supplier"].ToString();
                row.Cells[ColCostPrice].Value = ToMoney(dr["CostPrice"]);
                row.Cells[ColMarkup].Value = ToNumber(dr["Markup"]);
                row.Cells[ColSellPrice].Value = ToMoney(dr["Price"]);
            }
            dr.Close();
            cn.Close();
        }

        private void cbSupplier_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cbSupplier_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void LinGenerate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GetRefeNo();
        }

        private void LinProduct_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ProductStockIn productStock = new ProductStockIn(this);
            productStock.ShowDialog();
        }

    
        private void btnEntry_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvStockIn.Rows.Count > 0)
                {
                    if (MessageBox.Show("Are you sure you want to save this records?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        for (int i = 0; i < dgvStockIn.Rows.Count; i++)
                        {
                            int qty = int.Parse(dgvStockIn.Rows[i].Cells["Column5"].Value.ToString()); // Qty
                            string pcode = dgvStockIn.Rows[i].Cells["Column2"].Value.ToString(); // Pcode
                            string stockInId = dgvStockIn.Rows[i].Cells["Column9"].Value.ToString(); // Id

                            // 1️⃣ Update product quantity
                            cn.Open();
                            cm = new SqlCommand("UPDATE tbProduct SET qty = qty + @qty WHERE pcode=@pcode", cn);
                            cm.Parameters.AddWithValue("@qty", qty);
                            cm.Parameters.AddWithValue("@pcode", pcode);
                            cm.ExecuteNonQuery();
                            cn.Close();

                            // 2️⃣ Update stockin status only
                            cn.Open();
                            cm = new SqlCommand("UPDATE tbStockIn SET status='Done' WHERE id=@id", cn);
                            cm.Parameters.AddWithValue("@id", stockInId);
                            cm.ExecuteNonQuery();
                            cn.Close();
                        }

                        Clear();
                        LoadStockIn();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void Clear()
        {
            txtRefNo.Clear();
            txtStockInBy.Clear();
            dtStockIn.Value = DateTime.Now;
        }

        private void dgvStockIn_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            string colName = dgvStockIn.Columns[e.ColumnIndex].Name;
            if (colName == "Delete")
            {
                if (MessageBox.Show("Remove this item?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    cm = new SqlCommand("DELETE FROM tbStockIn WHERE id='" + dgvStockIn.Rows[e.RowIndex].Cells[1].Value.ToString() + "'", cn);
                    cm.ExecuteNonQuery();
                    cn.Close();
                    MessageBox.Show("Item has been successfully removed", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadStockIn();
                }
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            try
            {
                int i = 0;
                dgvInStockHistory.Rows.Clear();
                cn.Open();
                cm = new SqlCommand(@"
                    SELECT
                        s.id,
                        s.refno,
                        s.pcode,
                        p.pdesc,
                        s.qty,
                        s.sdate,
                        s.stockinby,
                        s.status,
                        ISNULL(sup.supplier,'') AS supplier,
                        ISNULL(s.CostPrice, p.costprice) AS CostPrice,
                        ISNULL(s.Markup, p.markup) AS Markup,
                        ISNULL(s.Price, p.price) AS Price
                    FROM tbStockIn s
                    INNER JOIN tbProduct p ON p.pcode = s.pcode
                    LEFT JOIN tbSupplier sup ON sup.id = s.supplierid
                    WHERE CAST(s.sdate AS date) BETWEEN @from AND @to AND s.status = 'Done'
                    ORDER BY s.sdate DESC, s.id DESC", cn);
                cm.Parameters.AddWithValue("@from", dtFrom.Value.Date);
                cm.Parameters.AddWithValue("@to", dtTo.Value.Date);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    int r = dgvInStockHistory.Rows.Add();
                    var row = dgvInStockHistory.Rows[r];
                    row.Cells["dataGridViewTextBoxColumn1"].Value = i;
                    row.Cells["dataGridViewTextBoxColumn2"].Value = dr["id"].ToString();
                    row.Cells["dataGridViewTextBoxColumn3"].Value = dr["refno"].ToString();
                    row.Cells["dataGridViewTextBoxColumn4"].Value = dr["pcode"].ToString();
                    row.Cells["dataGridViewTextBoxColumn5"].Value = dr["pdesc"].ToString();
                    row.Cells["dataGridViewTextBoxColumn6"].Value = dr["qty"].ToString();
                    row.Cells[ColCostPriceHist].Value = ToMoney(dr["CostPrice"]);
                    row.Cells[ColMarkupHist].Value = ToNumber(dr["Markup"]);
                    row.Cells[ColSellPriceHist].Value = ToMoney(dr["Price"]);
                    row.Cells["dataGridViewTextBoxColumn7"].Value = DateTime.Parse(dr["sdate"].ToString()).ToShortDateString();
                    row.Cells["dataGridViewTextBoxColumn8"].Value = dr["stockinby"].ToString();
                    row.Cells["dataGridViewTextBoxColumn9"].Value = dr["supplier"].ToString();

                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cbSupplier_TextChanged(object sender, EventArgs e)
        {
            cn.Open();
            cm = new SqlCommand("SELECT * FROM tbSupplier WHERE supplier LIKE '" + cbSupplier.Text + "'", cn);
            dr = cm.ExecuteReader();
            dr.Read();
            if (dr.HasRows)
            {
                lblId.Text = dr["id"].ToString();
                txtConPerson.Text = dr["contactperson"].ToString();
                txtAddress.Text = dr["address"].ToString();

            }
            dr.Close();
            cn.Close();
        }

  
        private void txtBarcode_KeyPress(object sender, KeyPressEventArgs e)
        {

        }



        //public void ScanBarcode(string barcode)
        //{
        //    try
        //    {
        //        string pcode = "";

        //        // 1️⃣ Get product code from barcode
        //        cn.Open();
        //        cm = new SqlCommand("SELECT pcode FROM tbProduct WHERE barcode=@barcode", cn);
        //        cm.Parameters.AddWithValue("@barcode", barcode);
        //        dr = cm.ExecuteReader();

        //        if (dr.Read())
        //            pcode = dr["pcode"].ToString();

        //        dr.Close();
        //        cn.Close();

        //        if (pcode == "")
        //        {
        //            MessageBox.Show("Product not found", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //            return;
        //        }

        //        // 2️⃣ Ask the user for quantity
        //        int qtyToAdd = 1; // default
        //        using (Form f = new Form())
        //        {
        //            f.Width = 250;
        //            f.Height = 140;
        //            f.Text = "Enter Quantity";
        //            f.StartPosition = FormStartPosition.CenterParent;
        //            f.FormBorderStyle = FormBorderStyle.FixedDialog;
        //            f.MaximizeBox = false;
        //            f.MinimizeBox = false;

        //            Label lbl = new Label() { Text = "Quantity:", Location = new Point(30, 20), AutoSize = true };
        //            TextBox txt = new TextBox() { Text = "1", Location = new Point(30, 45), Width = 170 };
        //            Button btnOk = new Button() { Text = "OK", Location = new Point(80, 80), DialogResult = DialogResult.OK };

        //            f.Controls.Add(lbl);
        //            f.Controls.Add(txt);
        //            f.Controls.Add(btnOk);
        //            f.AcceptButton = btnOk;

        //            if (f.ShowDialog() == DialogResult.OK)
        //            {
        //                if (!int.TryParse(txt.Text, out qtyToAdd) || qtyToAdd <= 0)
        //                    qtyToAdd = 1; // fallback if invalid input
        //            }
        //        }

        //        // 3️⃣ Check if product already exists in StockIn with status 'Pending'
        //        cn.Open();
        //        cm = new SqlCommand("SELECT id, qty FROM tbStockIn WHERE refno=@refno AND pcode=@pcode AND status='Pending'", cn);
        //        cm.Parameters.AddWithValue("@refno", txtRefNo.Text);
        //        cm.Parameters.AddWithValue("@pcode", pcode);
        //        dr = cm.ExecuteReader();

        //        if (dr.Read())
        //        {
        //            int id = Convert.ToInt32(dr["id"]);
        //            int existingQty = Convert.ToInt32(dr["qty"]);
        //            dr.Close();
        //            cn.Close();

        //            // 4️⃣ Update quantity with the user input
        //            cn.Open();
        //            cm = new SqlCommand("UPDATE tbStockIn SET qty = @newQty WHERE id=@id", cn);
        //            cm.Parameters.AddWithValue("@newQty", existingQty + qtyToAdd);
        //            cm.Parameters.AddWithValue("@id", id);
        //            cm.ExecuteNonQuery();
        //            cn.Close();
        //        }
        //        else
        //        {
        //            dr.Close();
        //            cn.Close();

        //            // 5️⃣ Insert new record
        //            cn.Open();
        //            cm = new SqlCommand(
        //                "INSERT INTO tbStockIn (refno, pcode, qty, sdate, stockinby, supplierid, status) " +
        //                "VALUES (@refno, @pcode, @qty, @sdate, @stockinby, @supplierid, 'Pending')", cn);
        //            cm.Parameters.AddWithValue("@refno", txtRefNo.Text);
        //            cm.Parameters.AddWithValue("@pcode", pcode);
        //            cm.Parameters.AddWithValue("@qty", qtyToAdd);
        //            cm.Parameters.AddWithValue("@sdate", dtStockIn.Value);
        //            cm.Parameters.AddWithValue("@stockinby", txtStockInBy.Text);
        //            cm.Parameters.AddWithValue("@supplierid", lblId.Text);
        //            cm.ExecuteNonQuery();
        //            cn.Close();
        //        }

        //        // 6️⃣ Refresh the DataGridView
        //        LoadStockIn();
        //    }
        //    catch (Exception ex)
        //    {
        //        cn.Close();
        //        MessageBox.Show(ex.Message, stitle);
        //    }
        //}
        public void ScanBarcode(string barcode)
        {
            try
            {
                string pcode = "";

                cn.Open();
                cm = new SqlCommand("SELECT pcode FROM tbProduct WHERE barcode=@barcode", cn);
                cm.Parameters.AddWithValue("@barcode", barcode);
                dr = cm.ExecuteReader();

                if (dr.Read())
                    pcode = dr["pcode"].ToString();

                dr.Close();
                cn.Close();

                if (pcode == "")
                {
                    MessageBox.Show("Product not found", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ✅ Prompt user for quantity
                int qtyToAdd = 1; // default
                using (Form f = new Form())
                {
                    f.Width = 250;
                    f.Height = 140;
                    f.Text = "Enter Quantity";
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.FormBorderStyle = FormBorderStyle.FixedDialog;
                    f.MaximizeBox = false;
                    f.MinimizeBox = false;

                    Label lbl = new Label() { Text = "Quantity:", Location = new Point(30, 20), AutoSize = true };
                    TextBox txt = new TextBox() { Text = "1", Location = new Point(30, 45), Width = 170 };
                    Button btnOk = new Button() { Text = "OK", Location = new Point(80, 80), DialogResult = DialogResult.OK };

                    f.Controls.Add(lbl);
                    f.Controls.Add(txt);
                    f.Controls.Add(btnOk);
                    f.AcceptButton = btnOk;

                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        if (!int.TryParse(txt.Text, out qtyToAdd) || qtyToAdd <= 0)
                            qtyToAdd = 1; // fallback if invalid input
                    }
                }

                // ✅ Use the same logic as manual add
                ScanBarcodeManual(pcode, qtyToAdd);
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle);
            }
        }
        public void ScanBarcodeManual(string pcode, int qtyToAdd)
        {
            try
            {
                // check if product already exists in StockIn with status 'Pending'
                cn.Open();
                cm = new SqlCommand("SELECT id, qty FROM tbStockIn WHERE refno=@refno AND pcode=@pcode AND status='Pending'", cn);
                cm.Parameters.AddWithValue("@refno", txtRefNo.Text);
                cm.Parameters.AddWithValue("@pcode", pcode);
                dr = cm.ExecuteReader();

                if (dr.Read())
                {
                    int id = Convert.ToInt32(dr["id"]);
                    int existingQty = Convert.ToInt32(dr["qty"]);
                    dr.Close();
                    cn.Close();

                    cn.Open();
                    cm = new SqlCommand("UPDATE tbStockIn SET qty = @newQty WHERE id=@id", cn);
                    cm.Parameters.AddWithValue("@newQty", existingQty + qtyToAdd);
                    cm.Parameters.AddWithValue("@id", id);
                    cm.ExecuteNonQuery();
                    cn.Close();
                }
                else
                {
                    dr.Close();
                    cn.Close();

                    // Pull default pricing from product master (does NOT overwrite product later)
                    var pricing = GetProductPricing(pcode);

                    cn.Open();
                    cm = new SqlCommand(
                        "INSERT INTO tbStockIn (refno, pcode, qty, sdate, stockinby, supplierid, status, CostPrice, Markup, Price) " +
                        "VALUES (@refno, @pcode, @qty, @sdate, @stockinby, @supplierid, 'Pending', @cost, @markup, @price)", cn);
                    cm.Parameters.AddWithValue("@refno", txtRefNo.Text);
                    cm.Parameters.AddWithValue("@pcode", pcode);
                    cm.Parameters.AddWithValue("@qty", qtyToAdd);
                    cm.Parameters.AddWithValue("@sdate", dtStockIn.Value);
                    cm.Parameters.AddWithValue("@stockinby", txtStockInBy.Text);
                    cm.Parameters.AddWithValue("@supplierid", lblId.Text);

                    cm.Parameters.AddWithValue("@cost", pricing.cost);
                    cm.Parameters.AddWithValue("@markup", pricing.markup);
                    cm.Parameters.AddWithValue("@price", pricing.price);
                    cm.ExecuteNonQuery();
                    cn.Close();
                }

                LoadStockIn();
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle);
            }
        }

        
        private void EnsurePricingColumnsForGrid(DataGridView grid, bool editable)
        {
            // Make only the pending grid editable. History grids remain read-only.
            if (editable)
                grid.ReadOnly = false;

            // If columns were added in Designer, they will already exist.
            // We still enforce formatting, read-only rules, and column order.

            // Determine where pricing columns should appear:
            // - In pending dgvStockIn: after Qty (Column5)
            // - In history dgvInStockHistory: after Qty (dataGridViewTextBoxColumn6)
            int desiredStartIndex = -1;
            if (grid.Columns.Contains("Column5"))
                desiredStartIndex = grid.Columns["Column5"].Index + 1;
            else if (grid.Columns.Contains("dataGridViewTextBoxColumn6"))
                desiredStartIndex = grid.Columns["dataGridViewTextBoxColumn6"].Index + 1;

            // If we couldn't detect, fall back to before Delete (or end)
            if (desiredStartIndex < 0)
                desiredStartIndex = grid.Columns.Contains("Delete") ? grid.Columns["Delete"].Index : grid.Columns.Count;

            // Use different column Names for history grid to avoid duplicate component Name errors.
            string costName = (grid == dgvInStockHistory) ? ColCostPriceHist : ColCostPrice;
            string markupName = (grid == dgvInStockHistory) ? ColMarkupHist : ColMarkup;
            string priceName = (grid == dgvInStockHistory) ? ColSellPriceHist : ColSellPrice;

            // Ensure the columns exist (in case someone removed them from designer)
            EnsureTextColumn(grid, costName, "Cost Price", editable, "#,##0.00", desiredStartIndex + 0);
            EnsureTextColumn(grid, markupName, "Markup %", editable, "#,##0.##", desiredStartIndex + 1);
            EnsureTextColumn(grid, priceName, "Price", editable, "#,##0.00", desiredStartIndex + 2, forceReadOnly: (grid == dgvInStockHistory));

            // Keep description visible if it exists
            try
            {
                if (grid.Columns.Contains("Column4"))
                {
                    grid.Columns["Column4"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    grid.Columns["Column4"].MinimumWidth = 180;
                }
                else if (grid.Columns.Contains("dataGridViewTextBoxColumn5"))
                {
                    // history grid description column
                    grid.Columns["dataGridViewTextBoxColumn5"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    grid.Columns["dataGridViewTextBoxColumn5"].MinimumWidth = 180;
                }
            }
            catch { }

            // Finally, ensure Delete image column remains the last column if present
            try
            {
                if (grid.Columns.Contains("Delete"))
                    grid.Columns["Delete"].DisplayIndex = grid.Columns.Count - 1;
            }
            catch { }
        }

        private void EnsureTextColumn(DataGridView grid, string name, string header, bool editable, string format, int desiredDisplayIndex, bool forceReadOnly = false)
        {
            if (!grid.Columns.Contains(name))
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = name,
                    HeaderText = header,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                };
                grid.Columns.Add(col);
            }

            var c = grid.Columns[name];
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            c.DefaultCellStyle.Format = format;

            if (forceReadOnly)
                c.ReadOnly = true;
            else
                c.ReadOnly = !editable;

            // Put the column in the right place (after Qty)
            try
            {
                // DisplayIndex reorders without breaking the underlying column collection.
                // Guard against invalid indexes.
                if (desiredDisplayIndex >= 0 && desiredDisplayIndex < grid.Columns.Count)
                    c.DisplayIndex = desiredDisplayIndex;
            }
            catch { }
        }

        private void dgvStockIn_CellValidating_Pricing(object sender, DataGridViewCellValidatingEventArgs e)
        {
            try
            {
                string colName = dgvStockIn.Columns[e.ColumnIndex].Name;
                if (colName != ColCostPrice && colName != ColMarkup && colName != ColSellPrice) return;

                var s = Convert.ToString(e.FormattedValue);
                if (string.IsNullOrWhiteSpace(s))
                {
                    dgvStockIn.Rows[e.RowIndex].ErrorText = "";
                    return;
                }

                if (!decimal.TryParse(s, out var v) || v < 0)
                {
                    e.Cancel = true;
                    dgvStockIn.Rows[e.RowIndex].ErrorText = "Please enter a valid non-negative number.";
                }
                else
                {
                    dgvStockIn.Rows[e.RowIndex].ErrorText = "";
                }
            }
            catch { }
        }

        private void dgvStockIn_CellEndEdit_Pricing(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string colName = dgvStockIn.Columns[e.ColumnIndex].Name;
                if (colName != ColCostPrice && colName != ColMarkup && colName != ColSellPrice) return;

                var row = dgvStockIn.Rows[e.RowIndex];
                if (row.IsNewRow) return;
                if (row.Cells["Column9"].Value == null) return; // id

                int stockInId = Convert.ToInt32(row.Cells["Column9"].Value);

                decimal cost = ToDecimal(row.Cells[ColCostPrice].Value);
                decimal markup = ToDecimal(row.Cells[ColMarkup].Value);
                decimal price = ToDecimal(row.Cells[ColSellPrice].Value);

                // Decide which direction to calculate based on which column user edited.
                if (colName == ColSellPrice)
                {
                    // User entered price -> calculate markup
                    markup = CalcMarkupPercent(cost, price);
                    row.Cells[ColMarkup].Value = markup.ToString("#,##0.##");
                }
                else if (colName == ColMarkup)
                {
                    // User entered markup -> calculate price
                    price = CalcSellingPrice(cost, markup);
                    row.Cells[ColSellPrice].Value = price.ToString("#,##0.00");
                }
                else if (colName == ColCostPrice)
                {
                    // If user already typed a price but markup is empty/zero, derive markup.
                    // Otherwise, derive price from markup.
                    bool hasPrice = !string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[ColSellPrice].Value));
                    bool hasMarkup = !string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[ColMarkup].Value));

                    if (hasPrice && (!hasMarkup || markup == 0m))
                    {
                        markup = CalcMarkupPercent(cost, price);
                        row.Cells[ColMarkup].Value = markup.ToString("#,##0.##");
                    }
                    else
                    {
                        price = CalcSellingPrice(cost, markup);
                        row.Cells[ColSellPrice].Value = price.ToString("#,##0.00");
                    }
                }

                // Persist all pricing fields
                cn.Open();
                cm = new SqlCommand(@"UPDATE tbStockIn SET CostPrice=@cost, Markup=@markup, Price=@price WHERE id=@id", cn);
                cm.Parameters.AddWithValue("@cost", cost);
                cm.Parameters.AddWithValue("@markup", markup);
                cm.Parameters.AddWithValue("@price", price);
                cm.Parameters.AddWithValue("@id", stockInId);
                cm.ExecuteNonQuery();
                cn.Close();
            }
            catch
            {
                try { cn.Close(); } catch { }
            }
        }

        private decimal CalcMarkupPercent(decimal cost, decimal price)
        {
            if (cost <= 0m) return 0m;
            decimal markup = ((price - cost) / cost) * 100m;
            return Math.Round(markup, 2);
        }

        private void Dgv_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Avoid the default popup and keep the UI usable.
            // (We fix the root cause by mapping cells by name, but this is a safe guard.)
            e.ThrowException = false;
            e.Cancel = true;
        }

        private decimal CalcSellingPrice(decimal cost, decimal markupPercent)
        {
            decimal selling = cost + (cost * markupPercent / 100m);
            return Math.Round(selling, 2);
        }

        private (decimal cost, decimal markup, decimal price) GetProductPricing(string pcode)
        {
            decimal cost = 0m, markup = 0m, price = 0m;
            cn.Open();
            cm = new SqlCommand("SELECT costprice, markup, price FROM tbProduct WHERE pcode=@pcode", cn);
            cm.Parameters.AddWithValue("@pcode", pcode);
            dr = cm.ExecuteReader();
            if (dr.Read())
            {
                cost = dr["costprice"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["costprice"]);
                markup = dr["markup"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["markup"]);
                if (dr["price"] != DBNull.Value)
                    price = Convert.ToDecimal(dr["price"]);
                else
                    price = CalcSellingPrice(cost, markup);
            }
            dr.Close();
            cn.Close();

            if (price <= 0m)
                price = CalcSellingPrice(cost, markup);
            return (Math.Round(cost, 2), Math.Round(markup, 2), Math.Round(price, 2));
        }

        private decimal ToDecimal(object v)
        {
            if (v == null || v == DBNull.Value) return 0m;
            decimal.TryParse(v.ToString(), out var d);
            return d;
        }

        private string ToMoney(object v)
        {
            if (v == null || v == DBNull.Value) return "0.00";
            if (decimal.TryParse(v.ToString(), out var d))
                return d.ToString("#,##0.00");
            return "0.00";
        }

        private string ToNumber(object v)
        {
            if (v == null || v == DBNull.Value) return "0";
            if (decimal.TryParse(v.ToString(), out var d))
                return d.ToString("#,##0.##");
            return "0";
        }

        private void AddProductHistoryTab()
        {
            try
            {
                if (metroTabControl1 == null) return;

                _tabProductHistory = new TabPage("Product History");
                _tabProductHistory.BackColor = Color.White;

                var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White };
                var lbl = new Label { Text = "Scan Barcode:", AutoSize = true, Location = new Point(12, 16) };
                _txtHistoryScan = new TextBox { Width = 260, Location = new Point(110, 12) };
                var btn = new Button { Text = "Search", Width = 90, Location = new Point(380, 10) };

                btn.Click += (s, e) => LoadProductStockInHistory(_txtHistoryScan.Text.Trim());
                _txtHistoryScan.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        LoadProductStockInHistory(_txtHistoryScan.Text.Trim());
                    }
                };

                topPanel.Controls.Add(lbl);
                topPanel.Controls.Add(_txtHistoryScan);
                topPanel.Controls.Add(btn);

                _dgvProductHistory = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AllowUserToAddRows = false,
                    RowHeadersVisible = false,
                    BackgroundColor = Color.White,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
                };

                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "No", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefNo", HeaderText = "Reference#", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Pcode", HeaderText = "Pcode", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Desc", HeaderText = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 180 });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = ColCostPrice, HeaderText = "Cost Price", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = ColMarkup, HeaderText = "Markup %", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = ColSellPrice, HeaderText = "Price", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "By", HeaderText = "Stock In By", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
                _dgvProductHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Supplier", HeaderText = "Supplier", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });

                _tabProductHistory.Controls.Add(_dgvProductHistory);
                _tabProductHistory.Controls.Add(topPanel);

                metroTabControl1.Controls.Add(_tabProductHistory);
            }
            catch { }
        }

        private void LoadProductStockInHistory(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return;

            string pcode = "";
            try
            {
                cn.Open();
                cm = new SqlCommand("SELECT pcode FROM tbProduct WHERE barcode=@barcode", cn);
                cm.Parameters.AddWithValue("@barcode", barcode);
                var obj = cm.ExecuteScalar();
                pcode = obj == null ? "" : obj.ToString();
                cn.Close();

                if (string.IsNullOrWhiteSpace(pcode))
                {
                    MessageBox.Show("Product not found", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int i = 0;
                _dgvProductHistory.Rows.Clear();
                cn.Open();
                cm = new SqlCommand(@"
                    SELECT
                        s.refno,
                        s.pcode,
                        p.pdesc,
                        s.qty,
                        s.sdate,
                        s.stockinby,
                        s.status,
                        ISNULL(sup.supplier,'') AS supplier,
                        ISNULL(s.CostPrice, p.costprice) AS CostPrice,
                        ISNULL(s.Markup, p.markup) AS Markup,
                        ISNULL(s.Price, p.price) AS Price
                    FROM tbStockIn s
                    INNER JOIN tbProduct p ON p.pcode = s.pcode
                    LEFT JOIN tbSupplier sup ON sup.id = s.supplierid
                    WHERE s.pcode = @pcode
                    ORDER BY s.sdate DESC, s.id DESC", cn);
                cm.Parameters.AddWithValue("@pcode", pcode);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    _dgvProductHistory.Rows.Add(
                        i,
                        DateTime.Parse(dr["sdate"].ToString()).ToShortDateString(),
                        dr["refno"].ToString(),
                        dr["pcode"].ToString(),
                        dr["pdesc"].ToString(),
                        dr["qty"].ToString(),
                        ToMoney(dr["CostPrice"]),
                        ToNumber(dr["Markup"]),
                        ToMoney(dr["Price"]),
                        dr["stockinby"].ToString(),
                        dr["status"].ToString(),
                        dr["supplier"].ToString()
                    );
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                try { cn.Close(); } catch { }
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



    }
}