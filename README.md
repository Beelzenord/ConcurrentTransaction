his software is a minimalistic API with small but robust logic that handles concurrent transactions. It relies on atomic operations to avert the risks of multithreaded environments, some of which includes race conditions and dirty reads. 

Has two API methods, a http get and http post. 
httpget /accounts/{iban}/transactions retrieves all transactions involving the iban string in its request parameter and also a timestamp representing the ‘snapshot’ of when that data was extracted. But only if there is no transaction underway.
Http post /payments represents a transaction request and expects a client id from the header with the body consisting of fields: Instructed Amount, Currency, Creditor and Debtor accounts. A successful flow will generate a ‘Transaction’ object and store it in the ConcurrentBag. Furthermore the method will return to the user a ‘receipt’ that is stored in the Payments TaskCompletionSource which also indicates that the transaction was successful. The processing time is two seconds and will automatically reject API post requests that contain matching fields for any debtor accounts, creditor accounts or clientId while the system is still processing. However concurrent API posts with unique parameters are accepted.     

Core logic
The transaction handler acts as a gatekeeper before allowing persisting payment requests to a ConcurrentBag which simulates data at rest.
A task attempting to perform a transaction (with mandatory fields) will attempt to add clientId to a concurrentDictionary as well as its creditor and debtor account details to a separate concurrentDictionary. While those entries exist, no other payment request with overlapping parameters can proceed, it is automatically rejected through an invalid operation exception. 
An internal thread safe counter owned by the Transaction Tracker is incremented while transaction is underway, through which the API will reject a get request before the transaction is complete and the internal counter is restored to 0. Transaction Counter acts like a low-level observer.        

Room for improvement
Allowing multiple clients linked to the same debtor account concurrently.
The scope only allows for unique payments, though it’s feasible to allow transactions from the same account to occur at the same time within the same scope of atomicity. One approach is by creating a relational system not unlike SQL or EF, by creating a dictionary object that has a tuple pair as a key. 
Possibly queuing 
Queuing in a similar fashion to the producer/consumer paradigm will make the system more efficient.

Other approaches and considerations

Semaphores
clientId and single semaphoreslim key-value mappings in concurrent dictionaries were considered and even used in earlier commits. Ultimately, it was determined that using the ‘key’ property sufficed in securing a thread safe execution. Additionally the use of semaphores added slight overhead and conflicted with the minimalistic ambition of this software.  
 Task Parallel Library (TPL)
Not considered as a viable option, since there was no need to create a broad pipeline chain. TPL is a robust tool for typically ‘producer’ and ‘consumer’ related problems. Since there is no scope for a ‘producer’ buffer, it’s not particularly vital to utilize this tool.     
