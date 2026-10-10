using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace SistFact.Negocio
{
    public class FactMgr
    {
        public static string cs = "Data Source=SRV-SQL01\\SQLEXPRESS;Initial Catalog=FACT_PROD;User Id=sa;Password=Fact2010!;";
        public static bool modoTest = false;
        public static Hashtable cache = new Hashtable();
        public static ArrayList hist = new ArrayList();
        public static int cnt = 0;

        public string cuit;
        public int tipo;
        public string fp;
        public int cuotas;
        public ArrayList items = new ArrayList();
        public double tot;
        public double tot2;
        public double tot3;
        public double d1;
        public double d2;
        public double iva1;
        public double iva2;
        public double iva3;
        public double perc;
        public double ii;
        public string err = "";
        public int nro;
        public string rs;
        public string prov;
        public int cat;
        public int cond;
        public bool flg;
        public bool flg2;
        private DataTable dt;
        private Hashtable h = new Hashtable();

        public FactMgr()
        {
            if (modoTest) CargarTest();
        }

        public int AgregarItem(string cod, double q)
        {
            if (cod == null || cod.Trim() == "")
            {
                err = "Codigo vacio";
                return -1;
            }
            if (q <= 0)
            {
                err = "Cantidad invalida";
                return -2;
            }
            if (q > 9999)
            {
                err = "Cantidad invalida";
                return -2;
            }
            double p = 0;
            double a = 0;
            int r = 0;
            int s = 0;
            string desc = "";
            if (modoTest)
            {
                Hashtable prd = (Hashtable)h["prd"];
                if (!prd.ContainsKey(cod.Trim().ToUpper()))
                {
                    err = "Producto inexistente: " + cod;
                    return -3;
                }
                object[] o = (object[])prd[cod.Trim().ToUpper()];
                desc = (string)o[0];
                p = (double)o[1];
                r = (int)o[2];
                s = (int)o[3];
            }
            else
            {
                SqlConnection cn = new SqlConnection(cs);
                try
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT DESCRIP, PRECIO, RUBRO, STOCK, ACTIVO FROM PRODUCTOS WHERE COD = '" + cod.Trim().ToUpper() + "'", cn);
                    SqlDataReader rd = cmd.ExecuteReader();
                    if (!rd.Read())
                    {
                        err = "Producto inexistente: " + cod;
                        rd.Close();
                        return -3;
                    }
                    if (Convert.ToInt32(rd["ACTIVO"]) == 0)
                    {
                        err = "Producto dado de baja";
                        rd.Close();
                        return -4;
                    }
                    desc = rd["DESCRIP"].ToString();
                    p = Convert.ToDouble(rd["PRECIO"]);
                    r = Convert.ToInt32(rd["RUBRO"]);
                    s = Convert.ToInt32(rd["STOCK"]);
                    rd.Close();
                }
                catch (Exception ex)
                {
                    err = ex.Message;
                    return -99;
                }
                finally
                {
                    cn.Close();
                }
            }
            if (s < q && r != 9)
            {
                err = "Stock insuficiente para " + cod;
                return -5;
            }
            if (r == 1 || r == 2) a = 0.21;
            else if (r == 3) a = 0.105;
            else if (r == 4) a = 0.27;
            else if (r == 5) a = 0;
            else if (r == 9) a = 0.21;
            else a = 0.21;
            for (int i = 0; i < items.Count; i++)
            {
                object[] x = (object[])items[i];
                if ((string)x[0] == cod.Trim().ToUpper())
                {
                    x[2] = (double)x[2] + q;
                    return items.Count;
                }
            }
            items.Add(new object[] { cod.Trim().ToUpper(), desc, q, p, a, r });
            return items.Count;
        }

        public bool Valid(string c)
        {
            if (c == null) return false;
            c = c.Replace("-", "").Replace(" ", "").Trim();
            if (c.Length != 11) return false;
            for (int i = 0; i < c.Length; i++)
                if (!char.IsDigit(c[i])) return false;
            string pf = c.Substring(0, 2);
            if (pf != "20" && pf != "23" && pf != "24" && pf != "27" && pf != "30" && pf != "33" && pf != "34") return false;
            int[] m = new int[] { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            int s = 0;
            for (int i = 0; i < 10; i++)
                s += int.Parse(c[i].ToString()) * m[i];
            int v = 11 - (s % 11);
            if (v == 11) v = 0;
            if (v == 10) v = 9;
            if (v != int.Parse(c[10].ToString())) return false;
            return true;
        }

        public int Proc(string c, int t, string f, int cq)
        {
            err = "";
            tot = 0; tot2 = 0; tot3 = 0; d1 = 0; d2 = 0; iva1 = 0; iva2 = 0; iva3 = 0; perc = 0; ii = 0;
            if (items.Count == 0)
            {
                err = "La factura no tiene items";
                return -1;
            }
            if (c == null) c = "";
            c = c.Replace("-", "").Replace(" ", "").Trim();
            if (c != "0" && !Valid(c))
            {
                err = "CUIT invalido";
                return -2;
            }
            cuit = c;
            tipo = t;
            fp = f == null ? "" : f.ToUpper().Trim();
            cuotas = cq;
            double sal = 0;
            double lim = 0;
            int bq = 0;
            if (c == "0")
            {
                rs = "CONSUMIDOR FINAL";
                prov = "BA";
                cat = 1;
                cond = 5;
            }
            else if (modoTest)
            {
                Hashtable cl = (Hashtable)h["cli"];
                if (!cl.ContainsKey(c))
                {
                    err = "Cliente inexistente";
                    return -3;
                }
                object[] o = (object[])cl[c];
                rs = (string)o[0];
                prov = (string)o[1];
                cat = (int)o[2];
                cond = (int)o[3];
                sal = (double)o[4];
                lim = (double)o[5];
                bq = (int)o[6];
            }
            else
            {
                if (cache.ContainsKey(c))
                {
                    dt = (DataTable)cache[c];
                }
                else
                {
                    SqlConnection cn = new SqlConnection(cs);
                    try
                    {
                        SqlDataAdapter da = new SqlDataAdapter("SELECT RAZON, PROV, CATEG, COND_IVA, SALDO, LIMITE, BLOQ FROM CLIENTES WHERE CUIT = '" + c + "'", cn);
                        dt = new DataTable();
                        da.Fill(dt);
                        cache[c] = dt;
                    }
                    catch (Exception ex)
                    {
                        err = "Error de base: " + ex.Message;
                        return -99;
                    }
                    finally
                    {
                        cn.Close();
                    }
                }
                if (dt.Rows.Count == 0)
                {
                    err = "Cliente inexistente";
                    return -3;
                }
                rs = dt.Rows[0]["RAZON"].ToString();
                prov = dt.Rows[0]["PROV"].ToString().Trim();
                cat = Convert.ToInt32(dt.Rows[0]["CATEG"]);
                cond = Convert.ToInt32(dt.Rows[0]["COND_IVA"]);
                sal = Convert.ToDouble(dt.Rows[0]["SALDO"]);
                lim = Convert.ToDouble(dt.Rows[0]["LIMITE"]);
                bq = Convert.ToInt32(dt.Rows[0]["BLOQ"]);
            }
            if (bq == 1 && !flg2)
            {
                err = "Cliente bloqueado";
                return -4;
            }
            if (t == 1 && cond != 1)
            {
                err = "Factura A solo para Responsables Inscriptos";
                return -5;
            }
            if (t == 6 && cond == 1)
            {
                err = "Cliente RI debe llevar factura A";
                return -5;
            }
            if (t != 1 && t != 6)
            {
                err = "Tipo de comprobante no habilitado";
                return -5;
            }
            if (fp != "EF" && fp != "TC" && fp != "TD" && fp != "CC" && fp != "CH")
            {
                err = "Forma de pago invalida";
                return -6;
            }
            if (fp != "TC" && cq > 1)
            {
                err = "Cuotas solo con tarjeta de credito";
                return -6;
            }
            if (cq < 1 || cq > 12)
            {
                err = "Cantidad de cuotas invalida";
                return -6;
            }
            if (fp == "CC" && c == "0")
            {
                err = "Cuenta corriente requiere cliente identificado";
                return -6;
            }
            double b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0;
            for (int i = 0; i < items.Count; i++)
            {
                object[] x = (object[])items[i];
                double q = (double)x[2];
                double p = (double)x[3];
                double a = (double)x[4];
                int r = (int)x[5];
                double st = q * p;
                if (r != 5)
                {
                    if (q >= 100) st = st * 0.90;
                    else if (q >= 50) st = st * 0.95;
                    else if (q >= 10) st = st * 0.98;
                }
                if (r == 4) b4 += st;
                if (a == 0.21) b1 += st;
                else if (a == 0.105) b2 += st;
                else if (a == 0.27) b3 += st;
                else b0 += st;
                tot += st;
            }
            double pd = 0;
            if (cat == 2) pd = 0.03;
            else if (cat == 3) pd = 0.05;
            else if (cat == 4)
            {
                if (tot > 50000) pd = 0.05;
                else pd = 0.08;
            }
            if (fp == "EF" && tot > 100) pd += 0.05;
            if (fp == "TD") pd += 0.02;
            if (DateTime.Now.DayOfWeek == DayOfWeek.Wednesday && cat >= 2) pd += 0.02;
            if (DateTime.Now.Month == 12 && DateTime.Now.Day >= 15 && DateTime.Now.Day <= 24) pd += 0.03;
            if (pd > 0.15 && !flg2) pd = 0.15;
            d1 = Math.Round(tot * pd, 2);
            tot2 = tot - d1;
            double k = 1 - pd;
            iva1 = Math.Round(b1 * k * 0.21, 2);
            iva2 = Math.Round(b2 * k * 0.105, 2);
            iva3 = Math.Round(b3 * k * 0.27, 2);
            if (t == 1)
            {
                if (prov == "BA" && tot2 > 3000) perc = tot2 * 0.03;
                else if (prov == "CABA") perc = tot2 * 0.025;
                else if (prov == "CBA" && cat != 1) perc = tot2 * 0.015;
                else if (prov == "SF") perc = tot2 * 0.018;
                perc = Math.Round(perc, 2);
            }
            ii = Math.Round(b4 * 0.08, 2);
            tot3 = tot2 + iva1 + iva2 + iva3 + perc + ii;
            if (fp == "TC" && cq > 1)
            {
                if (cq <= 3) d2 = tot3 * 0.06;
                else if (cq <= 6) d2 = tot3 * 0.12;
                else d2 = tot3 * 0.018 * cq;
                d2 = Math.Round(d2, 2);
                tot3 += d2;
            }
            tot3 = Math.Round(tot3, 2);
            if (fp == "CC" && sal + tot3 > lim && !flg2)
            {
                err = "Supera limite de credito. Disponible: " + (lim - sal).ToString("N2");
                return -8;
            }
            if (c == "0" && tot3 > 1000)
            {
                err = "Monto superior al permitido para consumidor final sin identificar";
                return -9;
            }
            if (tot3 <= 0)
            {
                err = "Total invalido";
                return -10;
            }
            if (modoTest)
            {
                cnt++;
                nro = 1000 + cnt;
                hist.Add(new object[] { nro, DateTime.Now, tot3, t, c, false, items });
                if (fp == "CC")
                {
                    object[] o = (object[])((Hashtable)h["cli"])[c];
                    o[4] = (double)o[4] + tot3;
                }
                for (int i = 0; i < items.Count; i++)
                {
                    object[] x = (object[])items[i];
                    object[] pr = (object[])((Hashtable)h["prd"])[(string)x[0]];
                    if ((int)x[5] != 9) pr[3] = (int)pr[3] - (int)(double)x[2];
                }
                flg = true;
                return 0;
            }
            SqlConnection cn2 = new SqlConnection(cs);
            SqlTransaction tr = null;
            try
            {
                cn2.Open();
                tr = cn2.BeginTransaction();
                SqlCommand cm = new SqlCommand("SELECT ISNULL(MAX(NRO),0)+1 FROM FACTURAS WHERE TIPO = " + t, cn2, tr);
                nro = Convert.ToInt32(cm.ExecuteScalar());
                cm.CommandText = "INSERT INTO FACTURAS (NRO, TIPO, FECHA, CUIT, RAZON, FP, CUOTAS, SUBTOT, DESC1, NETO, IVA21, IVA105, IVA27, PERC, IMPINT, RECARGO, TOTAL, ANULADA) VALUES ("
                    + nro + ", " + t + ", '" + DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + "', '" + c + "', '" + rs + "', '" + fp + "', " + cq + ", "
                    + tot.ToString().Replace(",", ".") + ", " + d1.ToString().Replace(",", ".") + ", " + tot2.ToString().Replace(",", ".") + ", "
                    + iva1.ToString().Replace(",", ".") + ", " + iva2.ToString().Replace(",", ".") + ", " + iva3.ToString().Replace(",", ".") + ", "
                    + perc.ToString().Replace(",", ".") + ", " + ii.ToString().Replace(",", ".") + ", " + d2.ToString().Replace(",", ".") + ", "
                    + tot3.ToString().Replace(",", ".") + ", 0)";
                cm.ExecuteNonQuery();
                for (int i = 0; i < items.Count; i++)
                {
                    object[] x = (object[])items[i];
                    cm.CommandText = "INSERT INTO FACTURAS_ITEMS (NRO, TIPO, RENGLON, COD, DESCRIP, CANT, PRECIO, ALIC) VALUES (" + nro + ", " + t + ", " + (i + 1) + ", '" + x[0] + "', '" + x[1] + "', "
                        + x[2].ToString().Replace(",", ".") + ", " + x[3].ToString().Replace(",", ".") + ", " + x[4].ToString().Replace(",", ".") + ")";
                    cm.ExecuteNonQuery();
                    if ((int)x[5] != 9)
                    {
                        cm.CommandText = "UPDATE PRODUCTOS SET STOCK = STOCK - " + x[2].ToString().Replace(",", ".") + " WHERE COD = '" + x[0] + "'";
                        cm.ExecuteNonQuery();
                    }
                }
                if (fp == "CC")
                {
                    cm.CommandText = "UPDATE CLIENTES SET SALDO = SALDO + " + tot3.ToString().Replace(",", ".") + " WHERE CUIT = '" + c + "'";
                    cm.ExecuteNonQuery();
                }
                tr.Commit();
                flg = true;
            }
            catch (Exception ex)
            {
                try { tr.Rollback(); }
                catch { }
                err = "Error al grabar: " + ex.Message;
                return -99;
            }
            finally
            {
                cn2.Close();
            }
            return 0;
        }

        public double Recalc()
        {
            double t = 0;
            for (int i = 0; i < items.Count; i++)
            {
                object[] x = (object[])items[i];
                double q = (double)x[2];
                double st = q * (double)x[3];
                if (q >= 100) st = st * 0.9;
                else if (q >= 50) st = st * 0.95;
                else if (q >= 10) st = st * 0.98;
                t += Math.Round(st, 2);
            }
            return t;
        }

        public string Imprimir()
        {
            if (!flg) return "";
            StringBuilder sb = new StringBuilder();
            string l = "";
            if (tipo == 1) l = "A";
            else if (tipo == 6) l = "B";
            else l = "?";
            sb.AppendLine("==========================================================");
            sb.AppendLine("  FACTURA " + l + "                       Nro: 0001-" + nro.ToString("00000000"));
            sb.AppendLine("  Fecha: " + DateTime.Now.ToString("dd/MM/yyyy"));
            sb.AppendLine("==========================================================");
            sb.AppendLine("  Cliente: " + rs);
            if (cuit != "0") sb.AppendLine("  CUIT: " + cuit.Substring(0, 2) + "-" + cuit.Substring(2, 8) + "-" + cuit.Substring(10));
            sb.AppendLine("  Cond. IVA: " + (cond == 1 ? "Responsable Inscripto" : cond == 4 ? "Exento" : cond == 6 ? "Monotributo" : "Consumidor Final"));
            string fpd = "";
            switch (fp)
            {
                case "EF": fpd = "Efectivo"; break;
                case "TC": fpd = "Tarjeta de credito" + (cuotas > 1 ? " (" + cuotas + " cuotas)" : ""); break;
                case "TD": fpd = "Tarjeta de debito"; break;
                case "CC": fpd = "Cuenta corriente"; break;
                case "CH": fpd = "Cheque"; break;
            }
            sb.AppendLine("  Forma de pago: " + fpd);
            sb.AppendLine("----------------------------------------------------------");
            sb.AppendLine(string.Format("  {0,-6}{1,-24}{2,6}{3,10}{4,11}", "Cod", "Descripcion", "Cant", "P.Unit", "Importe"));
            for (int i = 0; i < items.Count; i++)
            {
                object[] x = (object[])items[i];
                double pu = (double)x[3];
                if (tipo == 6) pu = pu * (1 + (double)x[4]);
                double im = pu * (double)x[2];
                string d = (string)x[1];
                if (d.Length > 22) d = d.Substring(0, 22);
                sb.AppendLine(string.Format("  {0,-6}{1,-24}{2,6}{3,10:N2}{4,11:N2}", x[0], d, x[2], pu, im));
            }
            sb.AppendLine("----------------------------------------------------------");
            if (tipo == 1)
            {
                sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "Subtotal", Recalc()));
                if (d1 > 0) sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "Bonificacion", -d1));
                sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "Neto gravado", tot2));
                if (iva1 > 0) sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "IVA 21%", iva1));
                if (iva2 > 0) sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "IVA 10,5%", iva2));
                if (iva3 > 0) sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "IVA 27%", iva3));
                if (perc > 0) sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "Percepcion IIBB " + prov, perc));
                if (ii > 0) sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "Impuestos internos", ii));
            }
            else
            {
                if (d1 > 0) sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "Bonificacion", -Math.Round(d1 * 1.21, 2)));
                if (ii > 0) sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "Impuestos internos", ii));
            }
            if (d2 > 0) sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "Recargo financiero", d2));
            sb.AppendLine(string.Format("  {0,-40}{1,15:N2}", "TOTAL", tot3));
            sb.AppendLine("==========================================================");
            return sb.ToString();
        }

        public bool Anular(int n, int t, string m)
        {
            err = "";
            if (m == null || m.Trim().Length < 10)
            {
                err = "Debe indicar un motivo (minimo 10 caracteres)";
                return false;
            }
            if (modoTest)
            {
                for (int i = 0; i < hist.Count; i++)
                {
                    object[] o = (object[])hist[i];
                    if ((int)o[0] == n && (int)o[3] == t)
                    {
                        if ((bool)o[5])
                        {
                            err = "El comprobante ya se encuentra anulado";
                            return false;
                        }
                        if ((DateTime.Now - (DateTime)o[1]).TotalDays > 15)
                        {
                            err = "No se pueden anular comprobantes de mas de 15 dias";
                            return false;
                        }
                        o[5] = true;
                        ArrayList its = (ArrayList)o[6];
                        for (int j = 0; j < its.Count; j++)
                        {
                            object[] x = (object[])its[j];
                            object[] pr = (object[])((Hashtable)h["prd"])[(string)x[0]];
                            if ((int)x[5] != 9) pr[3] = (int)pr[3] + (int)(double)x[2];
                        }
                        return true;
                    }
                }
                err = "Comprobante inexistente";
                return false;
            }
            SqlConnection cn = new SqlConnection(cs);
            try
            {
                cn.Open();
                SqlCommand cm = new SqlCommand("SELECT FECHA, ANULADA, FP, CUIT, TOTAL FROM FACTURAS WHERE NRO = " + n + " AND TIPO = " + t, cn);
                SqlDataReader rd = cm.ExecuteReader();
                if (!rd.Read())
                {
                    err = "Comprobante inexistente";
                    rd.Close();
                    return false;
                }
                DateTime fe = Convert.ToDateTime(rd["FECHA"]);
                bool an = Convert.ToInt32(rd["ANULADA"]) == 1;
                string fpa = rd["FP"].ToString();
                string cu = rd["CUIT"].ToString();
                double to = Convert.ToDouble(rd["TOTAL"]);
                rd.Close();
                if (an)
                {
                    err = "El comprobante ya se encuentra anulado";
                    return false;
                }
                if ((DateTime.Now - fe).TotalDays > 15)
                {
                    err = "No se pueden anular comprobantes de mas de 15 dias";
                    return false;
                }
                cm.CommandText = "UPDATE FACTURAS SET ANULADA = 1, MOTIVO = '" + m + "' WHERE NRO = " + n + " AND TIPO = " + t;
                cm.ExecuteNonQuery();
                SqlDataAdapter da = new SqlDataAdapter("SELECT COD, CANT FROM FACTURAS_ITEMS WHERE NRO = " + n + " AND TIPO = " + t, cn);
                DataTable di = new DataTable();
                da.Fill(di);
                foreach (DataRow dr in di.Rows)
                {
                    cm.CommandText = "UPDATE PRODUCTOS SET STOCK = STOCK + " + dr["CANT"].ToString().Replace(",", ".") + " WHERE COD = '" + dr["COD"] + "' AND RUBRO <> 9";
                    cm.ExecuteNonQuery();
                }
                if (fpa == "CC")
                {
                    cm.CommandText = "UPDATE CLIENTES SET SALDO = SALDO - " + to.ToString().Replace(",", ".") + " WHERE CUIT = '" + cu + "'";
                    cm.ExecuteNonQuery();
                    cache.Remove(cu);
                }
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return false;
            }
            finally
            {
                cn.Close();
            }
            return true;
        }

        public double TotDia(DateTime d)
        {
            double r = 0;
            if (modoTest)
            {
                foreach (object[] o in hist)
                {
                    if (((DateTime)o[1]).Date == d.Date && !(bool)o[5])
                    {
                        if ((int)o[3] == 1) r += (double)o[2];
                        else r += (double)o[2];
                    }
                }
                return r;
            }
            SqlConnection cn = new SqlConnection(cs);
            try
            {
                cn.Open();
                SqlCommand cm = new SqlCommand("SELECT ISNULL(SUM(TOTAL),0) FROM FACTURAS WHERE CONVERT(VARCHAR(8), FECHA, 112) = '" + d.ToString("yyyyMMdd") + "' AND ANULADA = 0", cn);
                r = Convert.ToDouble(cm.ExecuteScalar());
            }
            catch
            {
                r = -1;
            }
            finally
            {
                cn.Close();
            }
            return r;
        }

        private void CargarTest()
        {
            if (h.ContainsKey("prd")) return;
            if (!cache.ContainsKey("__prd"))
            {
                Hashtable p = new Hashtable();
                p["P001"] = new object[] { "Resma A4 75g", 28.50, 1, 500 };
                p["P002"] = new object[] { "Toner HP 85A", 389.00, 2, 40 };
                p["P003"] = new object[] { "Libro Contabilidad Basica", 125.00, 3, 25 };
                p["P004"] = new object[] { "Bebida energizante x6", 72.00, 4, 120 };
                p["P005"] = new object[] { "Leche entera x12", 58.80, 5, 60 };
                p["S001"] = new object[] { "Servicio tecnico (hora)", 150.00, 9, 0 };
                cache["__prd"] = p;
                Hashtable c = new Hashtable();
                c["30712345671"] = new object[] { "DISTRIBUIDORA EL SOL S.A.", "CABA", 3, 1, 12000.0, 50000.0, 0 };
                c["20123456786"] = new object[] { "PEREZ JUAN CARLOS", "BA", 1, 6, 0.0, 0.0, 0 };
                c["27234567891"] = new object[] { "GOMEZ MARIA LAURA", "CBA", 2, 5, 800.0, 1000.0, 1 };
                cache["__cli"] = c;
            }
            h["prd"] = cache["__prd"];
            h["cli"] = cache["__cli"];
        }
    }
}
