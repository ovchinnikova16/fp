using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using ResultOf;

namespace FileSenderRailway
{
	public class FileSender
	{
		private readonly ICryptographer cryptographer;
		private readonly IRecognizer recognizer;
		private readonly Func<DateTime> now;
		private readonly ISender sender;

		public FileSender(
			ICryptographer cryptographer,
			ISender sender,
			IRecognizer recognizer,
			Func<DateTime> now)
		{
			this.cryptographer = cryptographer;
			this.sender = sender;
			this.recognizer = recognizer;
			this.now = now;
		}

	    private Result<Document> PrepareFileToSend(FileContent file, X509Certificate certificate)
	    {
	        var recognizedFile = recognizer.Recognize(file);
            var result = new Result<Document>(null, recognizedFile);
	        if (!IsValidFormatVersion(result.Value))
	            result = new Result<Document>("Invalid format version", recognizedFile);
            if (result.IsSuccess && !IsValidTimestamp(result.Value))
	            result = new Result<Document>("Too old document", recognizedFile);
            result = result.Then(_ => result.Value.ChangeContent(cryptographer.Sign(result.Value.Content, certificate)));
            return result.RefineError("Can't prepare file to send");
        }

		public IEnumerable<FileSendResult> SendFiles(FileContent[] files, X509Certificate certificate)
		{
			foreach (var file in files)
			{
				//string errorMessage = null;
				//try
				//{
                    //Document doc = recognizer.Recognize(file);
                    //if (!IsValidFormatVersion(doc))
                    //    throw new FormatException("Invalid format version");
                    //if (!IsValidTimestamp(doc))
                    //    throw new FormatException("Too old document");
                    //doc = doc.ChangeContent(cryptographer.Sign(doc.Content, certificate));
				var result = PrepareFileToSend(file, certificate);
                if (!result.IsSuccess)
                    yield return new FileSendResult(file, result.Error);
                else
                {
                    sender.Send(result.Value);
                    yield return new FileSendResult(file);
                }
				//}
				//catch (FormatException e)
				//{
				//	errorMessage = "Can't prepare file to send. " + e.Message;
				//}
				//catch (InvalidOperationException e)
				//{
				//	errorMessage = "Can't send. " + e.Message;
				//}
				//yield return new FileSendResult(file, errorMessage);
			}
		}

		private bool IsValidFormatVersion(Document doc)
		{
			return doc.Format == "4.0" || doc.Format == "3.1";
		}

		private bool IsValidTimestamp(Document doc)
		{
			var oneMonthBefore = now().AddMonths(-1);
			return doc.Created > oneMonthBefore;
		}
	}
}