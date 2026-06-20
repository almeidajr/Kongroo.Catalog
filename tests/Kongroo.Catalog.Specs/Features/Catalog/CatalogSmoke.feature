@webapi @catalog
Feature: Catalog smoke
  The catalog service must be reachable and must enforce authentication on its endpoints.

Scenario: Health endpoint reports a successful status
  When the health endpoint is requested
  Then the response status code is 200

Scenario: Browsing the catalog without authentication is rejected
  When the games catalog is requested
  Then the response status code is 401

Scenario: An authenticated player can browse the catalog
  Given an authenticated player
  When the games catalog is requested
  Then the response status code is 200

Scenario: Placing an order without contact details on the token is rejected
  Given an authenticated player without contact details
  When the player places an order for a game
  Then the response status code is 401
