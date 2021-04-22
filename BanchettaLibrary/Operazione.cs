using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banchetta
{
    public class Operazione
    {
        public int IdOperazione { get;}
        public DateTime DataOperazione { get; } //col ? dopo DateTime indico che potrebbe non esserci e quindi essere nulla (vale per tutti i tipi valore)
        public string Causale { get; } //questa abbiamo detto nel db che può essere nulla ma la stringa ammette nullo già di suo
        public decimal Importo { get; }

        public Operazione(int idOperazione, DateTime dataOperazione, string causale, decimal importo)
        {
            IdOperazione = idOperazione;
            DataOperazione = dataOperazione;
            Causale = causale;
            Importo = importo;
        }

        public override string ToString()
        {
            return $"{DataOperazione}: {Importo}\t{Causale}";
        }


    }
}
