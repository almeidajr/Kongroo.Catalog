@webapi @catalog
Feature: Game reviews
  Players can review a game once and everyone can read the aggregated reviews.

Scenario: A player reviews a game and reads the summary
  Given an authenticated player
  And a game exists
  When the player submits a 5-star review
  Then the response status code is 201
  When the game reviews are requested
  Then the response status code is 200
  And the reviews summary shows 1 review with average 5

Scenario: A review with an invalid rating is rejected
  Given an authenticated player
  And a game exists
  When the player submits a 6-star review
  Then the response status code is 400

Scenario: A player cannot review the same game twice
  Given an authenticated player
  And a game exists
  When the player submits a 4-star review
  And the player submits a 3-star review
  Then the response status code is 409
