@webapi @catalog
Feature: Game search
  Customers find published games by words in their title or description, even with a typing error.

Scenario: A published game is searchable by a misspelled title until it is deleted
  Given a published game titled "Moonlit Orchard"
  And an authenticated player
  Then searching for "Monlit Orchard" eventually returns the game as the first hit
  When the game is deleted
  Then searching for "Monlit Orchard" eventually no longer returns the game
