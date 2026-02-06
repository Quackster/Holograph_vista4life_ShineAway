-- =====================================================
-- Holograph Emulator - Database Index Optimization Script
-- Generated based on query analysis from repository pattern
-- =====================================================

-- Run this script on your holodb database to add missing indexes
-- for optimal query performance.

-- =====================================================
-- USERS TABLE INDEXES
-- =====================================================

-- Index for user login by username (very frequent)
ALTER TABLE `users` ADD INDEX `idx_users_name` (`name`);

-- Index for SSO ticket authentication
ALTER TABLE `users` ADD INDEX `idx_users_ticket_sso` (`ticket_sso`);

-- Index for IP-based lookups (ban checks, user search)
ALTER TABLE `users` ADD INDEX `idx_users_ipaddress_last` (`ipaddress_last`);

-- Index for rank-based queries
ALTER TABLE `users` ADD INDEX `idx_users_rank` (`rank`);

-- =====================================================
-- ROOMS TABLE INDEXES
-- =====================================================

-- Index for room lookup by owner (navigator, room management)
ALTER TABLE `rooms` ADD INDEX `idx_rooms_owner` (`owner`);

-- Index for category-based room listings (navigator)
ALTER TABLE `rooms` ADD INDEX `idx_rooms_category` (`category`);

-- Index for room model lookups
ALTER TABLE `rooms` ADD INDEX `idx_rooms_model` (`model`);

-- Composite index for category with visitor sorting
ALTER TABLE `rooms` ADD INDEX `idx_rooms_category_visitors` (`category`, `visitors_now` DESC);

-- =====================================================
-- FURNITURE TABLE INDEXES
-- =====================================================

-- Index for furniture by owner (hand/inventory queries)
ALTER TABLE `furniture` ADD INDEX `idx_furniture_ownerid` (`ownerid`);

-- Index for furniture by room (room loading)
ALTER TABLE `furniture` ADD INDEX `idx_furniture_roomid` (`roomid`);

-- Index for furniture by template (catalogue lookups)
ALTER TABLE `furniture` ADD INDEX `idx_furniture_tid` (`tid`);

-- Composite index for hand items (owner + not in room)
ALTER TABLE `furniture` ADD INDEX `idx_furniture_hand` (`ownerid`, `roomid`);

-- Index for teleporter lookups
ALTER TABLE `furniture` ADD INDEX `idx_furniture_teleportid` (`teleportid`);

-- =====================================================
-- FURNITURE RELATED TABLES
-- =====================================================

-- furniture_presents - lookup by item ID
ALTER TABLE `furniture_presents` ADD INDEX `idx_furniture_presents_itemid` (`itemid`);

-- furniture_moodlight - already has composite PK, add roomid only index
ALTER TABLE `furniture_moodlight` ADD INDEX `idx_furniture_moodlight_roomid` (`roomid`);

-- =====================================================
-- USER BANS TABLE INDEXES
-- =====================================================

-- Index for ban check by user ID
ALTER TABLE `users_bans` ADD INDEX `idx_users_bans_userid` (`userid`);

-- Index for ban check by IP address
ALTER TABLE `users_bans` ADD INDEX `idx_users_bans_ipaddress` (`ipaddress`);

-- Index for expired ban cleanup
ALTER TABLE `users_bans` ADD INDEX `idx_users_bans_date_expire` (`date_expire`);

-- =====================================================
-- ROOM ACCESS TABLES
-- =====================================================

-- room_bans - composite index for ban checks
ALTER TABLE `room_bans` ADD INDEX `idx_room_bans_roomid_userid` (`roomid`, `userid`);

-- room_votes - composite index for vote checks
ALTER TABLE `room_votes` ADD INDEX `idx_room_votes_roomid_userid` (`roomid`, `userid`);

-- room_rights - composite index (userid index already exists, add roomid)
ALTER TABLE `room_rights` ADD INDEX `idx_room_rights_roomid` (`roomid`);

-- =====================================================
-- USER FAVORITES TABLE
-- =====================================================

-- Index for favorite rooms by user
ALTER TABLE `users_favouriterooms` ADD INDEX `idx_users_favouriterooms_userid` (`userid`);

-- Composite index for favorite room checks
ALTER TABLE `users_favouriterooms` ADD INDEX `idx_users_favouriterooms_userid_roomid` (`userid`, `roomid`);

-- =====================================================
-- GROUPS TABLES
-- =====================================================

-- groups_memberships - index for user's groups
ALTER TABLE `groups_memberships` ADD INDEX `idx_groups_memberships_userid` (`userid`);

-- groups_memberships - index for group members
ALTER TABLE `groups_memberships` ADD INDEX `idx_groups_memberships_groupid` (`groupid`);

-- Composite for current group lookup
ALTER TABLE `groups_memberships` ADD INDEX `idx_groups_memberships_userid_current` (`userid`, `is_current`);

-- =====================================================
-- CATALOGUE TABLES
-- =====================================================

-- catalogue_deals - lookup by page
ALTER TABLE `catalogue_deals` ADD INDEX `idx_catalogue_deals_page` (`catalogue_id_page`);

-- catalogue_items - lookup by page
ALTER TABLE `catalogue_items` ADD INDEX `idx_catalogue_items_page` (`catalogue_id_page`);

-- catalogue_items - lookup by CCT name
ALTER TABLE `catalogue_items` ADD INDEX `idx_catalogue_items_cct` (`name_cct`);

-- catalogue_pages - lookup by index name
ALTER TABLE `catalogue_pages` ADD INDEX `idx_catalogue_pages_indexname` (`indexname`);

-- catalogue_pages - rank-filtered queries
ALTER TABLE `catalogue_pages` ADD INDEX `idx_catalogue_pages_minrank` (`minrank`);

-- =====================================================
-- USER IGNORES TABLE
-- =====================================================

-- Index for ignore list by user
ALTER TABLE `user_ignores` ADD INDEX `idx_user_ignores_userid` (`userid`);

-- Composite for ignore checks
ALTER TABLE `user_ignores` ADD INDEX `idx_user_ignores_userid_ignoreid` (`userid`, `ignoreid`);

-- =====================================================
-- MESSENGER TABLES
-- =====================================================

-- messenger_friendrequests - lookup by recipient
ALTER TABLE `messenger_friendrequests` ADD INDEX `idx_messenger_friendrequests_to` (`userid_to`);

-- messenger_messages - lookup by recipient (if not covered by PK)
-- Already has composite PK on (userid, messageid)

-- =====================================================
-- ROOM BOTS TABLES
-- =====================================================

-- roombots - lookup by room
ALTER TABLE `roombots` ADD INDEX `idx_roombots_roomid` (`roomid`);

-- roombots_coords - lookup by bot
ALTER TABLE `roombots_coords` ADD INDEX `idx_roombots_coords_id` (`id`);

-- roombots_texts - lookup by bot
ALTER TABLE `roombots_texts` ADD INDEX `idx_roombots_texts_id` (`id`);

-- =====================================================
-- POLL TABLES
-- =====================================================

-- poll - lookup by room
ALTER TABLE `poll` ADD INDEX `idx_poll_rid` (`rid`);

-- poll_questions - lookup by poll
ALTER TABLE `poll_questions` ADD INDEX `idx_poll_questions_pid` (`pid`);

-- poll_answers - lookup by question
ALTER TABLE `poll_answers` ADD INDEX `idx_poll_answers_qid` (`qid`);

-- poll_results - lookup by poll and user
ALTER TABLE `poll_results` ADD INDEX `idx_poll_results_pid_userid` (`pid`, `userid`);

-- =====================================================
-- SOUNDMACHINE TABLES
-- =====================================================

-- soundmachine_songs - lookup by machine
ALTER TABLE `soundmachine_songs` ADD INDEX `idx_soundmachine_songs_machineid` (`machineid`);

-- soundmachine_playlists - lookup by machine
ALTER TABLE `soundmachine_playlists` ADD INDEX `idx_soundmachine_playlists_machineid` (`machineid`);

-- =====================================================
-- SYSTEM TABLES
-- =====================================================

-- system_chatlog - lookup by user
ALTER TABLE `system_chatlog` ADD INDEX `idx_system_chatlog_userid` (`userid`);

-- system_chatlog - lookup by room
ALTER TABLE `system_chatlog` ADD INDEX `idx_system_chatlog_roomid` (`roomid`);

-- system_stafflog - lookup by user
ALTER TABLE `system_stafflog` ADD INDEX `idx_system_stafflog_userid` (`userid`);

-- system_stafflog - lookup by target
ALTER TABLE `system_stafflog` ADD INDEX `idx_system_stafflog_targetid` (`targetid`);

-- =====================================================
-- ROOM MODEL TABLES
-- =====================================================

-- room_modeldata_triggers - lookup by model
ALTER TABLE `room_modeldata_triggers` ADD INDEX `idx_room_modeldata_triggers_model` (`model`);

-- =====================================================
-- GAMES TABLES
-- =====================================================

-- games_maps - lookup by lobby
ALTER TABLE `games_maps` ADD INDEX `idx_games_maps_lobbyid` (`lobbyid`);

-- games_maps_playerspawns - lookup by map
ALTER TABLE `games_maps_playerspawns` ADD INDEX `idx_games_maps_playerspawns_mapid` (`mapid`);

-- =====================================================
-- ROOM CATEGORIES TABLE
-- =====================================================

-- room_categories - lookup by parent (for subcategories)
ALTER TABLE `room_categories` ADD INDEX `idx_room_categories_parent` (`parent`);

-- room_categories - lookup by type
ALTER TABLE `room_categories` ADD INDEX `idx_room_categories_type` (`type`);

-- room_categories - rank-filtered queries
ALTER TABLE `room_categories` ADD INDEX `idx_room_categories_access_rank` (`access_rank_min`);

-- =====================================================
-- END OF INDEX SCRIPT
-- =====================================================

-- To verify indexes were created, run:
-- SHOW INDEX FROM users;
-- SHOW INDEX FROM rooms;
-- SHOW INDEX FROM furniture;
-- etc.
