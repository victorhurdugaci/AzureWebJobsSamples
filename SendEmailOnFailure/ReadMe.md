##Send email on failure

This samples demonstrates how to send an email when a queue message cannot be processed (because the message is invalid or because the function throws).

After 5 failed attempts to process the message, the message is moved to a poison queue. A second function listens to messages on the poison queue and will send an e-mail when it gets invoked.

In order to run the sample you need 
- [a Microsoft Azure Storage](http://azure.microsoft.com/en-us/documentation/services/storage/)
- [a SendGrid account](http://sendgrid.com/)

Contact:
http://twitter.com/victorhurdugaci