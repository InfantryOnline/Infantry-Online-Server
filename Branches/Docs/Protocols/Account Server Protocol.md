Free Infantry - Account Server Protocol
=======================================


Introduction
------------

The Account Server permits a user (client) to register and login with the game database, 
so that they may play Infantry Online. Prior to launching the game, the user must
authorize with the Account Server to receive the necessary session token.

The session token received by the user is required to be sent to the zone that the user
wishes to join.

This document describes the client-server protocol of the Account Server.


Transportation Layer
--------------------

The protocol is designed to use HTTP as the transportation layer. For added security,
HTTPS through SSL can be used.

This decision was made because HTTP is widespread and many different frameworks exist that
can make it simple to write a request handler (such as the Account Server) or a client 
program, which can either be desktop based, or a form on a website.

As such, all requests made are URLs, such as http://example.com/Accounts/New. See the API
section of the document for the actual URL structures.


Formats & Encodings
-------------------

* All requests and responses are expected to be UTF-8 encoded.
* All request and response payloads are sent as a JSON object.


Standard Status Codes
---------------------

The following is a list of the usual HTTP Status Codes used when a response is given.

### Acknowledgement

* 200 OK      - The request has been received successfully.

### Client-Side Errors

* 400 Bad Request - The payload is malformed.

### Server-Side Errors

* 500 Internal Error - There is a problem server-side.


Application Programming Interface
---------------------------------

The following is a list of the HTTP requests and responses, seen from the client's view.

### Sanity Check

Send the following request:

    GET http://example.com/Accounts

No return payload.

Return Codes:

* 200 OK - Server is running and in good condition.
* 500 Internal Error - There is a problem server-side.


### Account Registration

Send the following request:

    PUT http://example.com/Accounts

With example payload:

    {
        "Username": "User123",
        "PasswordHash": "7b502c3a1f48c8609ae212cdfb639dee39673f5e",
        "Email": "User123@example.com"
    }

Payload parameters:

* Username - The requested username; must be four or more characters.
* PasswordHash - A SHA1 hash calculation of the requested password.
* Email - The requested email; optional.

No payload is returned.

Return Codes:

* 201 Created - Account successfully registered.
* 400 Bad Request - The payload is malformed.
* 403 Forbidden - That username is already associated with an account.
* 406 Not Acceptable - The provided Username is invalid.
* 500 Internal Error - There is a problem server-side.


### Account Login

Send the following request:

    POST http://example.com/Accounts
    
With the example payload:

    {
        "Username": "User123",
        "PasswordHash": "7b502c3a1f48c8609ae212cdfb639dee39673f5e"
    }

Payload parameters:

* Username - The username associated with this account.
* PasswordHash - A SHA1 hash calculation of the password for this account.

Return payload:

    {
        "Username": "User123",
        "Email": "User123@example.com",
        "TicketId": "ce7bf373-3a8b-4b43-bfcb-ec57f032518c",
        "DateCreated": "5/16/2011 10:59:51 PM",
        "LastAccessed" "5/16/2011 10:59:51 PM",
        "Permission": 0
    }

Payload parameters:

* Username - The requested account's username.
* Email - The requested account's email address.
* TicketId - The unique ticket id associated with this account.
* DateCreated - The date of account's creation.
* LastAccessed - The last time this account was accessed.
* Permission - Ingame permission level.

Return Codes:

* 200 OK - Logged in successfully.
* 400 Bad Request - The payload is malformed.
* 404 Not Found - No account is found with those credentials.
* 500 Server Error - There is a problem server-side.


Server Design Standards
-----------------------

The following is a list of the things to keep in mind when developing 
your own Account Server.

### Unique Ticket Id Generation

A ticket is used to model a single playing session. The value of the ticket
should never be revealed except when absolutely needed (for example, launching 
the game).

To make tickets unlikely to be guessed, using a GUID is recommended. Nearly 
every language has a library for generating GUIDs.

For extra security, destroy the ticket once the session expires, and recreate it
upon next login.
