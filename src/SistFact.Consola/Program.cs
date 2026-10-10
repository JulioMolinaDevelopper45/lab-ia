using System;
using System.Collections.Generic;
using System.Text;
using SistFact.Negocio;

namespace SistFact.Consola
{
    class Program
    {
        static void Main(string[] args)
        {
            FactMgr.modoTest = true;
            if (args.Length > 0 && args[0].ToLower() == "/prod") FactMgr.modoTest = false;

            Console.WriteLine("SISTFACT v2.3.1 - Sistemas Integrales SRL");
            Console.WriteLine("Modo: " + (FactMgr.modoTest ? "TEST" : "PRODUCCION"));
            Console.WriteLine();

            int r;
            int nroA = 0;
            try
            {
                Console.WriteLine("[1] Factura A - Responsable Inscripto - Cuenta corriente");
                FactMgr f1 = new FactMgr();
                f1.AgregarItem("P001", 20);
                f1.AgregarItem("P002", 3);
                f1.AgregarItem("P004", 12);
                r = f1.Proc("30-71234567-1", 1, "CC", 1);
                if (r == 0)
                {
                    Console.WriteLine(f1.Imprimir());
                    nroA = f1.nro;
                }
                else Console.WriteLine("ERROR " + r + ": " + f1.err);

                Console.WriteLine("[2] Factura B - Monotributista - Tarjeta 6 cuotas");
                FactMgr f2 = new FactMgr();
                f2.AgregarItem("P003", 1);
                f2.AgregarItem("P005", 2);
                f2.AgregarItem("S001", 2);
                r = f2.Proc("20-12345678-6", 6, "TC", 6);
                if (r == 0) Console.WriteLine(f2.Imprimir());
                else Console.WriteLine("ERROR " + r + ": " + f2.err);

                Console.WriteLine("[3] Factura B - Consumidor final - Efectivo");
                FactMgr f3 = new FactMgr();
                f3.AgregarItem("P001", 4);
                f3.AgregarItem("P005", 1);
                r = f3.Proc("0", 6, "EF", 1);
                if (r == 0) Console.WriteLine(f3.Imprimir());
                else Console.WriteLine("ERROR " + r + ": " + f3.err);

                Console.WriteLine("[4] CUIT invalido");
                FactMgr f4 = new FactMgr();
                f4.AgregarItem("P001", 1);
                r = f4.Proc("20-12345678-0", 6, "EF", 1);
                Console.WriteLine(r == 0 ? "OK" : "ERROR " + r + ": " + f4.err);
                Console.WriteLine();

                Console.WriteLine("[5] Cliente bloqueado");
                FactMgr f5 = new FactMgr();
                f5.AgregarItem("P003", 2);
                r = f5.Proc("27234567891", 6, "EF", 1);
                Console.WriteLine(r == 0 ? "OK" : "ERROR " + r + ": " + f5.err);
                Console.WriteLine();

                Console.WriteLine("[6] Consumidor final sin identificar - monto alto");
                FactMgr f6 = new FactMgr();
                f6.AgregarItem("P002", 5);
                r = f6.Proc("0", 6, "EF", 1);
                Console.WriteLine(r == 0 ? "OK" : "ERROR " + r + ": " + f6.err);
                Console.WriteLine();

                Console.WriteLine("[7] Stock insuficiente");
                FactMgr f7 = new FactMgr();
                r = f7.AgregarItem("P002", 500);
                Console.WriteLine(r > 0 ? "OK" : "ERROR " + r + ": " + f7.err);
                Console.WriteLine();

                Console.WriteLine("Total facturado hoy: " + new FactMgr().TotDia(DateTime.Today).ToString("N2"));
                Console.WriteLine();

                if (nroA > 0)
                {
                    Console.WriteLine("[8] Anulacion de la factura A " + nroA);
                    FactMgr fa = new FactMgr();
                    if (!fa.Anular(nroA, 1, "error")) Console.WriteLine("ERROR: " + fa.err);
                    if (fa.Anular(nroA, 1, "Error en la carga de cantidades")) Console.WriteLine("Anulada OK");
                    else Console.WriteLine("ERROR: " + fa.err);
                    if (!fa.Anular(nroA, 1, "Error en la carga de cantidades")) Console.WriteLine("ERROR: " + fa.err);
                    Console.WriteLine();
                }

                Console.WriteLine("Total facturado hoy: " + new FactMgr().TotDia(DateTime.Today).ToString("N2"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error inesperado: " + ex.Message);
            }
        }
    }
}
