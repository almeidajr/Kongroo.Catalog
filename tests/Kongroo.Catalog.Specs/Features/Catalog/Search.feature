@webapi @catalog
Feature: Game search
  Customers find published games by words in their title or description, even with a typing error.

Scenario: A published game becomes searchable by a misspelled title
  Given a published game titled "Moonlit Orchard"
  And an authenticated player
  Then searching for "Monlit Orchard" eventually returns the game as the first hit
