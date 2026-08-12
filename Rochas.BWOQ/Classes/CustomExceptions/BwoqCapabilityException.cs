using System;

namespace Rochas.BWOQ
{
    /// <summary>
    /// Indica que uma expressão BWOQ válida não pode ser traduzida pelo modo
    /// de execução escolhido (SQL ANSI / GenericRepository), dado o contrato
    /// público de metadados disponível. A mensagem orienta o modo alternativo.
    /// </summary>
    public class BwoqCapabilityException : Exception
    {
        public BwoqCapabilityException(string message)
            : base(message)
        {
        }

        public BwoqCapabilityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}