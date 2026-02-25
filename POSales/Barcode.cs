//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Data.SqlClient;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Drawing.Printing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using Zen.Barcode;


//namespace POSales
//{
//    public partial class Barcode : Form
//    {

//        SqlConnection cn = new SqlConnection();
//        SqlCommand cm = new SqlCommand();
//        DBConnect dbcon = new DBConnect();
//        SqlDataReader dr;
//        string fname;
//        PrintDocument pd = new PrintDocument();

//        public Barcode()
//        {
//            InitializeComponent();
//            cn = new SqlConnection(dbcon.myConnection());
//            LoadProduct();
//            pd.PrintPage += new PrintPageEventHandler(this.PrintPage);
//            string productDescription = "";
//            string productPrice = "";

//        }

//        public void LoadProduct()
//        {
//            int i = 0;
//            dgvBarcode.Rows.Clear();
//            cm = new SqlCommand("SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.reorder FROM tbProduct AS p INNER JOIN tbBrand AS b ON b.id = p.bid INNER JOIN tbCategory AS c on c.id = p.cid WHERE CONCAT(p.pdesc, b.brand, c.category) LIKE '%" + txtSearch.Text + "%'", cn);
//            cn.Open();
//            dr = cm.ExecuteReader();
//            while (dr.Read())
//            {
//                i++;
//                dgvBarcode.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString());
//            }
//            dr.Close();
//            cn.Close();
//        }

//        private void txtSearch_TextChanged(object sender, EventArgs e)
//        {
//            LoadProduct();
//        }

//        private void dgvBarcode_CellContentClick(object sender, DataGridViewCellEventArgs e)
//        {
//            string colName = dgvBarcode.Columns[e.ColumnIndex].Name;
//            if (colName == "Select")
//            {
//                Code128BarcodeDraw barcode = BarcodeDrawFactory.Code128WithChecksum;
//                picBarcode.Image = barcode.Draw(dgvBarcode.Rows[e.RowIndex].Cells[2].Value.ToString(), 25, 1);
//                fname = dgvBarcode.Rows[e.RowIndex].Cells[1].Value.ToString();
//            }
//        }

//        private void btnSave_Click(object sender, EventArgs e)
//        {
//            SaveFileDialog savefile = new SaveFileDialog();
//            savefile.Title = " Save Barcode Image As";
//            savefile.FileName = fname;
//            savefile.Filter = "Image Flie(*.jpg,*.png)| *.jpg, *.png";
//            ImageFormat image = ImageFormat.Png;
//            if (savefile.ShowDialog() == DialogResult.OK)
//            {
//                string ftype = System.IO.Path.GetExtension(savefile.FileName);
//                switch (ftype)
//                {
//                    case ".jpg":
//                        image = ImageFormat.Jpeg;
//                        break;
//                }
//                picBarcode.Image.Save(savefile.FileName, image);
//            }
//            picBarcode.Image = null;
//        }
//        private void btnPrint_Click(object sender, EventArgs e)
//        {
//            PrintPreviewDialog preview = new PrintPreviewDialog();
//            PaperSize labelSize = new PaperSize("Label", 177, 98); // 4.5cm x 2.5cm
//            pd.DefaultPageSettings.PaperSize = labelSize;
//            pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
//            preview.Document = pd;
//            preview.ShowDialog();
//        }

//        private void PrintPage(object sender, PrintPageEventArgs e)
//        {
//            if (dgvBarcode.CurrentRow == null) return;

//            Graphics g = e.Graphics;

//            int labelWidth = pd.DefaultPageSettings.PaperSize.Width;   // ~177px
//            int labelHeight = pd.DefaultPageSettings.PaperSize.Height; // ~98px
//            int margin = 2;
//            int currentY = margin;

//            string barcodeNumber = dgvBarcode.CurrentRow.Cells[2].Value.ToString();
//            string description = dgvBarcode.CurrentRow.Cells[3].Value.ToString();
//            string price = dgvBarcode.CurrentRow.Cells[6].Value.ToString();

//            // --- Draw Barcode Image ---
//            if (picBarcode.Image != null)
//            {
//                int barcodeWidth = labelWidth - 2 * margin;
//                int barcodeHeight = picBarcode.Image.Height;  // keep original height
//                g.DrawImage(picBarcode.Image, margin, currentY, barcodeWidth, barcodeHeight);
//                currentY += barcodeHeight + 2;
//            }

//            // --- Barcode Number ---
//            Font numberFont = new Font("Arial", 7, FontStyle.Bold);
//            SizeF numberSize = g.MeasureString(barcodeNumber, numberFont);
//            g.DrawString(barcodeNumber, numberFont, Brushes.Black,
//                margin + (labelWidth - numberSize.Width) / 2, currentY);
//            currentY += 10;

//            // --- Description ---
//            Font descFont = new Font("Arial", 6);
//            RectangleF descRect = new RectangleF(margin, currentY, labelWidth, 12);
//            g.DrawString(description, descFont, Brushes.Black, descRect);
//            currentY += 12;

//            // --- Price ---
//            Font priceFont = new Font("Arial", 7, FontStyle.Bold);
//            SizeF priceSize = g.MeasureString(price, priceFont);
//            g.DrawString("Price: " + price, priceFont, Brushes.Black,
//                margin + (labelWidth - priceSize.Width) / 2, currentY);
//        }





//    }
//}
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Windows.Forms;
using Zen.Barcode;

namespace POSales
{
    public partial class Barcode : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        string fname;
        PrintDocument pd = new PrintDocument();
        Image currentBarcodeImage;

        public Barcode()
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            LoadProduct();

            // Attach PrintPage handler
            pd.PrintPage += new PrintPageEventHandler(PrintPage);
        }

        // Load products into the DataGridView
        public void LoadProduct()
        {
            int i = 0;
            dgvBarcode.Rows.Clear();
            cm = new SqlCommand(@"SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.reorder 
                                  FROM tbProduct AS p 
                                  INNER JOIN tbBrand AS b ON b.id = p.bid 
                                  INNER JOIN tbCategory AS c ON c.id = p.cid 
                                  WHERE CONCAT(p.pdesc, b.brand, c.category) LIKE @search", cn);
            cm.Parameters.AddWithValue("@search", "%" + txtSearch.Text + "%");
            cn.Open();
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                dgvBarcode.Rows.Add(i, dr["pcode"].ToString(), dr["barcode"].ToString(),
                                     dr["pdesc"].ToString(), dr["brand"].ToString(),
                                     dr["category"].ToString(), dr["price"].ToString(),
                                     dr["reorder"].ToString());
            }
            dr.Close();
            cn.Close();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadProduct();
        }

        // Generate barcode image when "Select" is clicked
        private void dgvBarcode_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            string colName = dgvBarcode.Columns[e.ColumnIndex].Name;
            if (colName == "Select")
            {
                string barcodeNumber = dgvBarcode.Rows[e.RowIndex].Cells[2].Value.ToString();

                // Generate a scannable barcode image
                Code128BarcodeDraw barcode = BarcodeDrawFactory.Code128WithChecksum;

                // Small module width for tiny label, enough height for scanner
                currentBarcodeImage = barcode.Draw(barcodeNumber, 40, 1); // 40px height, 1px module width
                picBarcode.Image = currentBarcodeImage;

                fname = barcodeNumber;
            }
        }

        // Save the barcode image
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (picBarcode.Image == null) return;

            SaveFileDialog savefile = new SaveFileDialog();
            savefile.Title = "Save Barcode Image As";
            savefile.FileName = fname;
            savefile.Filter = "Image File (*.jpg,*.png)|*.jpg;*.png";
            ImageFormat image = ImageFormat.Png;

            if (savefile.ShowDialog() == DialogResult.OK)
            {
                string ftype = System.IO.Path.GetExtension(savefile.FileName).ToLower();
                if (ftype == ".jpg") image = ImageFormat.Jpeg;

                picBarcode.Image.Save(savefile.FileName, image);
            }
        }

        // Print the selected barcode
        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (dgvBarcode.CurrentRow == null)
            {
                MessageBox.Show("Please select a product first.");
                return;
            }

            // Set label size: 4.5cm x 2.5cm (approx at 100dpi)
            PaperSize labelSize = new PaperSize("Label", 177, 98);
            pd.DefaultPageSettings.PaperSize = labelSize;
            pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            PrintPreviewDialog preview = new PrintPreviewDialog();
            preview.Document = pd;
            preview.ShowDialog();
        }

        // Draw the barcode + number + description + price
        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            if (dgvBarcode.CurrentRow == null || currentBarcodeImage == null) return;

            Graphics g = e.Graphics;
            int labelWidth = pd.DefaultPageSettings.PaperSize.Width;
            int margin = 2;
            int currentY = margin;

            string barcodeNumber = dgvBarcode.CurrentRow.Cells[2].Value.ToString();
            string description = dgvBarcode.CurrentRow.Cells[3].Value.ToString();
            string price = dgvBarcode.CurrentRow.Cells[6].Value.ToString();

            // Draw barcode image (do not resize!)
            int barcodeX = margin + (labelWidth - currentBarcodeImage.Width) / 2;
            g.DrawImage(currentBarcodeImage, barcodeX, currentY, currentBarcodeImage.Width, currentBarcodeImage.Height);
            currentY += currentBarcodeImage.Height + 2;

            // Draw barcode number
            Font numberFont = new Font("Arial", 6, FontStyle.Bold);
            SizeF numberSize = g.MeasureString(barcodeNumber, numberFont);
            g.DrawString(barcodeNumber, numberFont, Brushes.Black,
                         margin + (labelWidth - numberSize.Width) / 2, currentY);
            currentY += 8;

            // Draw description
            Font descFont = new Font("Arial", 5);
            RectangleF descRect = new RectangleF(margin, currentY, labelWidth, 10);
            g.DrawString(description, descFont, Brushes.Black, descRect);
            currentY += 10;

            // Draw price
            Font priceFont = new Font("Arial", 6, FontStyle.Bold);
            SizeF priceSize = g.MeasureString(price, priceFont);
            g.DrawString("Price: " + price, priceFont, Brushes.Black,
                         margin + (labelWidth - priceSize.Width) / 2, currentY);
        }
    }
}
