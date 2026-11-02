-- Third integration type: the Seva Sindhu EP0115 token API. For departments whose entry page checks the token against
-- Seva Sindhu (their /validate API), the token and the application reference must be issued by Seva Sindhu's own
-- /generate API, not by us. SevaOne calls /generate with the user and Seva Sindhu's service id (the catalogue's
-- sevasindhu_service_id), then posts the citizen's browser to the department URL with that token, as
-- Seva Sindhu does itself. The API address is a deployment setting (TokenIntegration:EdcsApi:BaseUrl), not per service.

INSERT INTO discovery.token_integration_type (type_key, display_name, description, fields_json) VALUES (
    'edcs_api',
    'Token from Seva Sindhu (EP0115 API)',
    'SevaOne asks the Seva Sindhu token API for a token and application reference (using the service''s Seva Sindhu service id), then the citizen''s browser posts UserId, ServiceId, edcsApplicationRefNo, assistedmode and Token to the department URL. The department checks the token and reports the acknowledgement to the Seva Sindhu API.',
    '{"landingUrl":{"required":true,"label":"Department URL (URL 1)","help":"Page that receives the POST form."},
      "sendMobile":{"label":"Also send mobile","help":"Only for departments that ask for it."},
      "sendDigilockerId":{"label":"Also send DigiLocker id","help":"Only for departments that ask for it."}}'::jsonb
) ON CONFLICT (type_key) DO NOTHING;
