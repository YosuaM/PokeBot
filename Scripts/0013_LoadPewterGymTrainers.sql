BEGIN TRANSACTION;

-- =========================================================
-- Pewter City Gym Trainers (Kanto Gym 1 - Brock)
--
-- This script seeds the trainers for the first gym.
-- Gym reference:
--   Gyms.Id = 1 (PewterGym, defined in 0004_LoadGyms)
--   Location: PewterCity (Id = 3, defined in 0001_LoadLocations)
--
-- Team composition uses Kanto Pokédex IDs from PokemonSpecies:
--   Pidgey     -> Id = 16
--   Rattata    -> Id = 19
--   Sandshrew  -> Id = 27
--   Geodude    -> Id = 74
--   Onix       -> Id = 95
--
-- Rewards use ItemTypes from 0006_LoadItemTypes:
--   POKE_BALL
--   GREAT_BALL
-- =========================================================

-- Optional: clear previous trainers for Pewter Gym so the script is idempotent
DELETE FROM GymTrainers WHERE GymId = 1;

-- 1) Camper Liam - simple rock-type introduction
INSERT INTO GymTrainers (
    GymId, Name, [Order],
    Pokemon1SpeciesId, Pokemon1Level,
    RewardMoney, RewardItemTypeId, RewardItemQuantity
)
VALUES (
    1, 'Camper Liam', 1,
    74, 8,
    200,
    (SELECT Id FROM ItemTypes WHERE Code = 'POKE_BALL'), 1
);

-- 2) Youngster Tim - ground-type preview
INSERT INTO GymTrainers (
    GymId, Name, [Order],
    Pokemon1SpeciesId, Pokemon1Level,
    RewardMoney, RewardItemTypeId, RewardItemQuantity
)
VALUES (
    1, 'Youngster Tim', 2,
    27, 9,
    220,
    NULL, 0
);

-- 3) Lass Ana - mixed normal/flying
INSERT INTO GymTrainers (
    GymId, Name, [Order],
    Pokemon1SpeciesId, Pokemon1Level,
    Pokemon2SpeciesId, Pokemon2Level,
    RewardMoney, RewardItemTypeId, RewardItemQuantity
)
VALUES (
    1, 'Lass Ana', 3,
    19, 9,
    16, 9,
    240,
    (SELECT Id FROM ItemTypes WHERE Code = 'POKE_BALL'), 1
);

-- 4) Hiker Bob - double rock
INSERT INTO GymTrainers (
    GymId, Name, [Order],
    Pokemon1SpeciesId, Pokemon1Level,
    Pokemon2SpeciesId, Pokemon2Level,
    RewardMoney, RewardItemTypeId, RewardItemQuantity
)
VALUES (
    1, 'Hiker Bob', 4,
    74, 10,
    95, 10,
    260,
    NULL, 0
);

-- 5) Gym Leader Brock (official character)
INSERT INTO GymTrainers (
    GymId, Name, [Order],
    Pokemon1SpeciesId, Pokemon1Level,
    Pokemon2SpeciesId, Pokemon2Level,
    RewardMoney, RewardItemTypeId, RewardItemQuantity
)
VALUES (
    1, 'Brock', 5,
    74, 12,
    95, 14,
    500,
    (SELECT Id FROM ItemTypes WHERE Code = 'GREAT_BALL'), 2
);

COMMIT;
