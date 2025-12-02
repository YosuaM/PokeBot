BEGIN TRANSACTION;

-- =========================================================
-- Assign locations to Kanto gyms
--
-- Gyms are defined in 0004_LoadGyms with fixed Ids 1..8
-- Locations are defined in 0001_LoadLocations with fixed Ids
--
-- Mapping:
--   PewterGym   (Id = 1) -> PewterCity    (LocationId = 3)
--   CeruleanGym (Id = 2) -> CeruleanCity  (LocationId = 4)
--   VermilionGym(Id = 3) -> VermilionCity (LocationId = 5)
--   CeladonGym  (Id = 4) -> CeladonCity   (LocationId = 7)
--   FuchsiaGym  (Id = 5) -> FuchsiaCity   (LocationId = 8)
--   SaffronGym  (Id = 6) -> SaffronCity   (LocationId = 9)
--   CinnabarGym (Id = 7) -> CinnabarIsland(LocationId = 10)
--   ViridianGym (Id = 8) -> ViridianCity  (LocationId = 2)
-- =========================================================

UPDATE Gyms SET LocationId = 3  WHERE Id = 1;  -- PewterGym   -> PewterCity
UPDATE Gyms SET LocationId = 4  WHERE Id = 2;  -- CeruleanGym -> CeruleanCity
UPDATE Gyms SET LocationId = 5  WHERE Id = 3;  -- VermilionGym-> VermilionCity
UPDATE Gyms SET LocationId = 7  WHERE Id = 4;  -- CeladonGym  -> CeladonCity
UPDATE Gyms SET LocationId = 8  WHERE Id = 5;  -- FuchsiaGym  -> FuchsiaCity
UPDATE Gyms SET LocationId = 9  WHERE Id = 6;  -- SaffronGym  -> SaffronCity
UPDATE Gyms SET LocationId = 10 WHERE Id = 7;  -- CinnabarGym -> CinnabarIsland
UPDATE Gyms SET LocationId = 2  WHERE Id = 8;  -- ViridianGym -> ViridianCity

-- Optionally ensure all gyms are open by default
UPDATE Gyms SET IsOpen = 1 WHERE Id BETWEEN 1 AND 8;

ROLLBACK;
