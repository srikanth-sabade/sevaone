-- A second selectable method alongside "edcs_api" (Seva Sindhu's own /redirect): the original method, where
-- SevaOne builds the form itself and posts it straight to the department's own URL. Same Seva Sindhu /generate
-- call either way; only who posts the token to the department differs. Only one can be active per service.

INSERT INTO discovery.token_integration_type (type_key, display_name, description, fields_json) VALUES (
    'edcs_api_form',
    'Token from Seva Sindhu (we post to the department)',
    'SevaOne asks the Seva Sindhu token API for a token and application reference (using the service''s Seva Sindhu service id), then SevaOne itself posts UserId, ServiceId, edcsApplicationRefNo, assistedmode and Token to the department URL -- one less hop than letting Seva Sindhu''s own redirect do it, but the department URL has to be entered here and kept correct.',
    '{"landingUrl":{"required":true,"label":"Department URL (URL 1)","help":"Page that receives the POST form."},
      "sendMobile":{"label":"Also send mobile","help":"Only for departments that ask for it."},
      "sendDigilockerId":{"label":"Also send DigiLocker id","help":"Only for departments that ask for it."}}'::jsonb
) ON CONFLICT (type_key) DO NOTHING;
