**Message to CeG: GetDocumentFromDigiLocker returns -2 (drafted 2026-09-26)**  
Status: a shorter version of this (no curls, no account details) was sent to CeG on 2026-09-27; awaiting their reply. This full version, with the curls, is the follow-up if they ask for proof. It contains the test account's real DigiLocker ID: redact it before committing or sharing beyond CeG.  
**Subject: GetDocumentFromDigiLocker returns "-2 Invalid Parameter" on the test department key (DeptKey 8139035611)**  
Dear CeG DigiLocker Gateway Team,  
We are integrating the DigiLocker Gateway (API Specifications v1.2) into the SevaOne citizen mobile app, using the test department credentials you issued. Sign-in, GetIssuedDocList and GetEAadharDetails work correctly. We cannot get GetDocumentFromDigiLocker to succeed and need your help.  
**What works:** GetIssuedDocList for a real, signed-in test account returns 4 documents (Aadhaar Card, Driving License, and 2 vehicle registrations). Our signing also reproduces the APIMac sample in your test-credentials sheet exactly.  
**What fails:** GetDocumentFromDigiLocker for the first document returned above.  
*A. Working call (* *GetIssuedDocList* *)*  
curl -i -X POST 'http://103.138.197.55/dlapi/API/CeGLockerAPI.svc/GetIssuedDocList' \  
   -H 'Content-Type: application/json' \  
   -d '{"DeptKey": "8139035611", "DigilockerID": "43ca190e-cfe0-11e9-ae46-9457a564506c", "APIMac": "CAbI/LWrJbR3H3NOipG1ZRimj7PVs5ND1jxiBgEZTcI="}'  
   
Response: HTTP 200, StatusCode 0, SUCCESS, 4 documents.  
*B. * *GetDocumentFromDigiLocker* *, body exactly as v1.2 documents it*  
curl -i -X POST 'http://103.138.197.55/dlapi/API/CeGLockerAPI.svc/GetDocumentFromDigiLocker' \  
   -H 'Content-Type: application/json' \  
   -d '{"DeptKey": "8139035611", "DigilockerID": "43ca190e-cfe0-11e9-ae46-9457a564506c", "DocURI": "in.gov.uidai-ADHAR-747ae1af7e5ffe609e516a04719c58b9", "FileType": 2, "APIMac": "FIEzBnhVIYmuuLaWNpAtf6+hAZwTv0lzgZN5ITK0V/E=", "TimeStamp": "2026-09-26 17:01:25"}'  
   
Response: HTTP 400, WCF "Request Error" page.  
*C. Same call wrapped in * *reqData* *, as the service's WSDL defines it*  
curl -i -X POST 'http://103.138.197.55/dlapi/API/CeGLockerAPI.svc/GetDocumentFromDigiLocker' \  
   -H 'Content-Type: application/json' \  
   -d '{"reqData": {"DeptKey": "8139035611", "DigilockerID": "43ca190e-cfe0-11e9-ae46-9457a564506c", "DocURI": "in.gov.uidai-ADHAR-747ae1af7e5ffe609e516a04719c58b9", "FileType": 2, "APIMac": "FIEzBnhVIYmuuLaWNpAtf6+hAZwTv0lzgZN5ITK0V/E=", "TimeStamp": "2026-09-26 17:01:25"}}'  
   
Response: HTTP 200, StatusCode -2, "Invalid Parameter".  
The APIMac is HMAC-SHA256 of DeptKey + DigilockerID + DocURI, base64, as documented. We also tried FileType 1, 2 and 3 and about a dozen TimeStamp formats, with and without the timestamp in the MAC. Every attempt returned -2.  
**Our questions:**  
1. Is GetDocumentFromDigiLocker enabled for our test department key (8139035611), or does it need production credentials or additional approval? It is not among the four APIs listed in the test-credentials sheet.  
2. Should the request be wrapped in reqData, as call C? The v1.2 document shows a flat body, which the server rejects with HTTP 400.  
3. What is the exact TimeStamp format and time zone, and is it part of the APIMac input?  
4. Could you share one working sample request and response for this API, or tell us which parameter -2 is objecting to?  
For the account above, the list returns only application/json and application/xml as available formats for each document, so we expect FileType 2 or 3 rather than PDF.  
Thank you.  
Regards,  
   
 Srikanth, SevaOne Mobile App Team  
