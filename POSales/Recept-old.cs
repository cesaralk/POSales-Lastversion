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
using Microsoft.Reporting.WinForms;

namespace POSales
{
    public partial class Recept : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        string store;
        string address;
        bool autoPrint = false;
        Cashier cashier;
        

        public Recept(Cashier cash)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            cashier = cash;
            LoadStore();
        }

        public void LoadStore()
        {
            cn.Open();
            cm = new SqlCommand("SELECT * FROM tbStore", cn);
            dr = cm.ExecuteReader();
            dr.Read();
            if(dr.HasRows)
            {
                store = dr["store"].ToString();
                address = dr["address"].ToString();
            }
            dr.Close();
            cn.Close();
        }

        private void Recept_Load(object sender, EventArgs e)
        {

            this.reportViewer1.RefreshReport();
        }
       

        public void LoadRecept(string pcash, string pchange)
        {
            ReportDataSource rptDataSourece;
            try
            {
                this.reportViewer1.LocalReport.ReportPath = Application.StartupPath + @"\Reports\rptRecept.rdlc";
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                SqlDataAdapter da = new SqlDataAdapter();

                cn.Open();
                da.SelectCommand = new SqlCommand("SELECT c.id, c.transno, c.pcode, c.price, c.qty, c.disc, c.total, c.sdate, c.status, p.pdesc FROM tbCart AS c INNER JOIN tbProduct AS p ON p.pcode=c.pcode WHERE c.transno LIKE '"+cashier.lblTranNo.Text+"'",cn);
                da.Fill(ds.Tables["dtRecept"]);
                cn.Close();

                ReportParameter pVatable = new ReportParameter("pVatable", cashier.lblVatable.Text);
                ReportParameter pVat = new ReportParameter("pVat", cashier.lblVat.Text);
                ReportParameter pDiscount = new ReportParameter("pDiscount", cashier.lblDiscount.Text);
                ReportParameter pTotal = new ReportParameter("pTotal", cashier.lblDisplayTotal.Text);
                ReportParameter pCash = new ReportParameter("pCash", pcash);
                ReportParameter pChange = new ReportParameter("pChange", pchange);
                ReportParameter pStore = new ReportParameter("pStore", store);
                ReportParameter pAddress = new ReportParameter("pAddress", address);
                ReportParameter pTransaction = new ReportParameter("pTransaction", "Invoice #: " + cashier.lblTranNo.Text);
                ReportParameter pCashier = new ReportParameter("pCashier", cashier.lblUsername.Text);

                reportViewer1.LocalReport.SetParameters(pVatable);
                reportViewer1.LocalReport.SetParameters(pVat);
                reportViewer1.LocalReport.SetParameters(pDiscount);
                reportViewer1.LocalReport.SetParameters(pTotal);
                reportViewer1.LocalReport.SetParameters(pCash);
                reportViewer1.LocalReport.SetParameters(pChange);
                reportViewer1.LocalReport.SetParameters(pStore);
                reportViewer1.LocalReport.SetParameters(pAddress);
                reportViewer1.LocalReport.SetParameters(pTransaction);
                reportViewer1.LocalReport.SetParameters(pCashier);

                rptDataSourece = new ReportDataSource("DataSet1", ds.Tables["dtRecept"]);
                reportViewer1.LocalReport.DataSources.Add(rptDataSourece);
                reportViewer1.SetDisplayMode(Microsoft.Reporting.WinForms.DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 30;
                autoPrint = true;
                reportViewer1.RefreshReport();

            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message);
            }

        }
        //public void PrintReceipt()
        //{
        //    reportViewer1.RefreshReport();

        //    // Print silently to default printer
        //    reportViewer1.PrintDialog();
        //}
        private void reportViewer1_RenderingComplete(object sender, RenderingCompleteEventArgs e)
        {
            if (autoPrint)
            {
                autoPrint = false;

                try
                {
                    // Render the report to EMF (metafile) pages
                    Warning[] warnings;
                    var streams = new System.Collections.Generic.List<System.IO.Stream>();
                    reportViewer1.LocalReport.Render(
                        "Image",
                        DeviceInfo(),
                        (name, fileNameExtension, encoding, mimeType, willSeek) =>
                        {
                            var stream = new System.IO.MemoryStream();
                            streams.Add(stream);
                            return stream;
                        },
                        out warnings);

                    foreach (var stream in streams)
                        stream.Position = 0;

                    // Send to printer
                    PrintStreams(streams);

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Printing error: " + ex.Message);
                }

                this.Dispose();
            }
        }

        // Device info: standard page size (A4)
        private string DeviceInfo()
        {
            return @"<DeviceInfo>
        <OutputFormat>EMF</OutputFormat>
        <PageWidth>8.27in</PageWidth>
        <PageHeight>11.69in</PageHeight>
        <MarginTop>0.25in</MarginTop>
        <MarginLeft>0.25in</MarginLeft>
        <MarginRight>0.25in</MarginRight>
        <MarginBottom>0.25in</MarginBottom>
    </DeviceInfo>";
        }

        // Print EMF streams
        //private void PrintStreams(System.Collections.Generic.List<System.IO.Stream> streams)
        //{
        //    System.Drawing.Printing.PrintDocument printDoc = new System.Drawing.Printing.PrintDocument();
        //    int currentPageIndex = 0;

        //    printDoc.PrintPage += (s, e) =>
        //    {
        //        var pageImage = System.Drawing.Image.FromStream(streams[currentPageIndex]);
        //        e.Graphics.DrawImage(pageImage, e.PageBounds);
        //        currentPageIndex++;
        //        e.HasMorePages = (currentPageIndex < streams.Count);
        //    };

        //    printDoc.Print();
        //}
        private void PrintStreams(System.Collections.Generic.List<System.IO.Stream> streams)
        {
            System.Drawing.Printing.PrintDocument printDoc = new System.Drawing.Printing.PrintDocument();
            int currentPageIndex = 0;

            printDoc.PrintPage += (s, e) =>
            {
                var pageImage = System.Drawing.Image.FromStream(streams[currentPageIndex]);
                e.Graphics.DrawImage(pageImage, e.PageBounds); // fit page
                currentPageIndex++;
                e.HasMorePages = (currentPageIndex < streams.Count);
            };

            // Dispose form AFTER printing completes
            printDoc.EndPrint += (s, e) =>
            {
                this.Invoke(new Action(() => this.Close()));
            };

            printDoc.Print();
        }






        public void PrintReceipt()
        {
            autoPrint = true;
            reportViewer1.RefreshReport();
        }

        private void Recept_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Escape)
            {
                this.Dispose();
            }
        }
    }
}
