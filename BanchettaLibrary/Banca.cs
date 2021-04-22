using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;

namespace Banchetta
{
        public static class Banca //una sorta di Data Access Layer
        { 
        //connessione che recupero ogni volta che mi serve (non la scrivo dentro ad un metodo perchè la usano tutti)
        static string _connectionString = ConfigurationManager.ConnectionStrings["Banchetta"].ConnectionString;

        //elimina un conto e tutte le sue operazioni (per esercizio, in modalità disconnessa)
        public static void EliminaConto(Conto c)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlDataAdapter da = new SqlDataAdapter("Select * from Conti", conn))
            {
                DataSet ds = new DataSet();

                da.Fill(ds, "Conti"); //fill e update devono avere gli stessi parametri
                
                DataTable tableConti = ds.Tables["Conti"];

                //2 metodi per eliminare una colonna, con find il record e cancellalo
                tableConti.PrimaryKey = new DataColumn[] { tableConti.Columns["IdConto"] }; //devo dirgli che idConto è la chiave primaria!
                                                                                            //vuole un array di colonne in cui gli specifico di quale tabella
                                                                                            //quale colonna deve essere la chiave primaria
                tableConti.Rows.Find(c.IdConto).Delete(); //cerca quella riga che ha come chiave primaria idConto e cancellala

                //oppure con un ciclo in cui individuo la singola colonna con l'id che chiedo e poi la cancello
                foreach (DataRow row in ds.Tables["Conti"].Rows)
                    if ((int)row["IdConto"] == c.IdConto) 
                    {
                        row.Delete();
                        break; 
                    }

                new SqlCommandBuilder(da); //crea i comandi per l'update del db

                da.Update(ds, "Conti");
            }

        }

        //restituisce una lista dei conti con eventualmente le operazioni RIVEDILO PERCHE' NON VA BENE
        public static List<Conto> ElencoConti(bool conOperazioni = false) //senza parametri è una proprietà, con è un metodo.
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("Select * from Conti", conn))
            {
                List<Conto> conti = new List<Conto>();

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                    conti.Add(new Conto((int)reader["IdConto"], reader["Intestatario"].ToString()));

                if (conOperazioni)
                    foreach (Conto c in conti)
                        RecuperaOperazioni(c);

                return conti;
            }



        }  

        static public Conto CreaConto(string intestatario)
        {
            
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("Insert into Conti (Intestatario) values (@intestatario); Select @@identity", conn)) //devo usare i parametri nelle query e usare il comdando dopo per valorizzarli
            {
                // comando che contiene anche la valorizzazione del parametro usato nella query sopra
                cmd.Parameters.AddWithValue("@intestatario", intestatario);
                conn.Open();
                int id = (int)(decimal)cmd.ExecuteScalar(); //recupero l'ID del nuovo conto inserito

                return new Conto(id, intestatario);
            }    
        }

        static public void VersaSulConto(Conto conto, DateTime dataOperazione, decimal importo, string causale)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("Insert into Operazioni (DataOperazione, Causale, Importo, IdConto) values (@dataOperazione, @causale, @importo, @idConto); Select @@identity", conn))
            {
                cmd.Parameters.AddWithValue("@dataOperazione", dataOperazione);
                cmd.Parameters.AddWithValue("@causale", causale);
                cmd.Parameters.AddWithValue("@importo", importo);
                cmd.Parameters.AddWithValue("@idConto", conto.IdConto); //recupero l'id da conto!

                conn.Open();
                int id = (int)(decimal)cmd.ExecuteScalar();

                Operazione op = new Operazione(id, dataOperazione, causale, importo);

                conto.Operazioni.Add(op);

            }
        }

        static public void PrelevaDalConto(Conto conto, DateTime dataOperazione, decimal importo, string causale)
        {
            VersaSulConto(conto, dataOperazione, -importo, causale);
        }

        public static void RecuperaOperazioni(Conto conto) //recupera le operazioni del conto e le aggiunge alla sua lista
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("Select * from Operazioni where IdConto=@idConto", conn))
            {
                cmd.Parameters.AddWithValue("@idConto", conto.IdConto);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read()) //per ogni riga letta dal reader, recupero tutte le info e le aggiungo alla lista delle operazioni del conto
                {
                    conto.Operazioni.Add(
                        new Operazione((int)reader["idOperazione"],
                        (DateTime)reader["DataOperazione"], 
                        reader["Causale"].ToString(), 
                        (decimal)reader["Importo"]));
                }

            }
        }

        //questo metodo lo faccio dicendogli di recuperare una stored procedure opportunamente realizzata di là
        public static Conto RecuperaConto(int idConto, bool conOperazioni = false) //NON MI dara' PERO' LE OPERAZIONI RELATIVE AL CONTO, aggiungo booleano operazioni che se le voglio metto true, altrimenti solo conto
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("RecuperaConto", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@idConto", idConto);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read()) //se il reader non trova il conto ritorna nullo (read vuole booleano), uso if e non while perchè ho un solo conto
                    return null;

                //altrimenti creo un conto a cui associo i valori letti dal reader e lo ritorno
                Conto c = new Conto((int)reader["IdConto"], reader["Intestatario"].ToString());

                if (conOperazioni) //se metto true a conOperazioni, il compilatore passerà di qua
                    RecuperaOperazioni(c);
                             
               return c;
                

            }
        }
        }
}
