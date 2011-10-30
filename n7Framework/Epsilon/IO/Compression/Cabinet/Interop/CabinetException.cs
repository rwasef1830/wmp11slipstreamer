using System;
using System.Collections.Generic;
using System.Text;

namespace Epsilon.IO.Compression.Cabinet
{
    internal class FciException : CabinetException
    {
        string errorCode;
        string cErrorCode;

        internal FciException(SafeMemoryBlock pErrorStruct, 
            ICollection<Exception> exceptions) : base("FciException", exceptions)
        {
            FCI.Error errorStruct = pErrorStruct.GetStructure<FCI.Error>();
            pErrorStruct.Close();
            this.errorCode = errorStruct.ErrorCode.ToString();
            this.cErrorCode = errorStruct.CErrorCode.ToString();
        }

        protected override string ErrorCode
        {
            get { return this.errorCode; }
        }

        protected override string CErrorCode
        {
            get { return this.cErrorCode; }
        }
    }

    internal class FdiException : CabinetException
    {
        string errorCode;
        string cErrorCode;

        internal FdiException(SafeMemoryBlock pErrorStruct,
            ICollection<Exception> exceptions) : base("FdiException", exceptions)
        {
            FDI.Error errorStruct = pErrorStruct.GetStructure<FDI.Error>();
            pErrorStruct.Close();
            this.errorCode = errorStruct.errorCode.ToString();
            this.cErrorCode = errorStruct.cErrorCode.ToString();
        }

        protected override string ErrorCode
        {
            get { return this.errorCode; }
        }

        protected override string CErrorCode
        {
            get { return this.cErrorCode; }
        }
    }

    public abstract class CabinetException : Exception
    {
        ICollection<Exception> _exceptions;
        string _exceptionName;

        protected abstract string ErrorCode { get; }
        protected abstract string CErrorCode { get; }

        protected CabinetException(string exceptionName, 
            ICollection<Exception> exceptions)
        {
            this._exceptions = exceptions;
            this._exceptionName = exceptionName;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(200);
            builder.AppendFormat("{0}: Error code: {1}:{2}.",
                this._exceptionName, this.ErrorCode, this.CErrorCode);
            builder.AppendLine();
            if (this._exceptions.Count > 0)
            {
                builder.AppendLine("---> Recorded exceptions in managed callbacks:");

                int number = 1;
                foreach (Exception ex in this._exceptions)
                {
                    builder.AppendFormat("{0}. ", number++);
                    builder.AppendLine(ex.ToString());
                    builder.AppendLine();
                }
                builder.Remove(builder.Length - Environment.NewLine.Length - 1,
                    Environment.NewLine.Length);
                builder.AppendLine("---> End of callback exceptions.");
            }
            builder.AppendLine();
            builder.AppendLine("Stack Trace:");
            builder.Append(this.StackTrace);
            return builder.ToString();
        }
    }
}
