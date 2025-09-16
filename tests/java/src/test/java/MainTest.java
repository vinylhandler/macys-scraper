import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import okhttp3.*;
import okhttp3.mockwebserver.MockResponse;
import okhttp3.mockwebserver.MockWebServer;
import okhttp3.mockwebserver.RecordedRequest;
import org.json.JSONObject;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Paths;

import static org.junit.jupiter.api.Assertions.*;

public class MainTest {
    private MockWebServer mockWebServer;
    private OkHttpClient client;
    private String mockResponseJson;

    @BeforeEach
    void setUp() throws IOException {
        mockWebServer = new MockWebServer();
        mockWebServer.start();
        
        client = new OkHttpClient.Builder().build();
        
        mockResponseJson = Files.readString(Paths.get("../shared/mock_responses.json"));
    }

    @AfterEach
    void tearDown() throws IOException {
        mockWebServer.shutdown();
    }

    @Test
    void testPayloadStructure() {
        JSONObject jsonObject = new JSONObject();
        jsonObject.put("source", "universal");
        jsonObject.put("url", "https://www.macys.com/shop/sale?id=3536&trackingid=407x733169&m_sc=sem&m_sb=google&m_tp=trademark&m_ac=google_trademark_international&m_ag=macy%27score_exact&m_cn=ggl_trademark_intl_lithuania_exact&m_pi=go_cmp-94807774_adg-154238318312_ad-674544461209_kwd-252677959_dev-c_ext-102882313401_prd-&gad_source=1&gclid=cj0kcqiayewrbhddarisagp1mwsg3z6ogoqrztdycjyqio5togc316ldkwuqkkbhrmiv4i_ho0gcjlkaagl6ealw_wcb");

        assertEquals("universal", jsonObject.getString("source"));
        assertTrue(jsonObject.getString("url").contains("macys.com"));
        assertEquals(2, jsonObject.length());
    }

    @Test
    void testAuthenticationSetup() throws IOException, InterruptedException {
        mockWebServer.enqueue(new MockResponse()
                .setBody(mockResponseJson)
                .setHeader("Content-Type", "application/json"));

        String username = "test_user";
        String password = "test_pass";

        Authenticator authenticator = (route, response) -> {
            String credential = Credentials.basic(username, password);
            return response.request().newBuilder()
                    .header("Authorization", credential)
                    .build();
        };

        OkHttpClient testClient = new OkHttpClient.Builder()
                .authenticator(authenticator)
                .build();

        JSONObject jsonObject = new JSONObject();
        jsonObject.put("source", "universal");
        jsonObject.put("url", "https://www.macys.com/shop/sale");

        MediaType mediaType = MediaType.parse("application/json; charset=utf-8");
        RequestBody body = RequestBody.create(jsonObject.toString(), mediaType);
        Request request = new Request.Builder()
                .url(mockWebServer.url("/v1/queries"))
                .post(body)
                .build();

        try (Response response = testClient.newCall(request).execute()) {
            assertEquals(200, response.code());
            
            RecordedRequest recordedRequest = mockWebServer.takeRequest();
            assertNotNull(recordedRequest.getHeader("Authorization"));
            assertTrue(recordedRequest.getHeader("Authorization").startsWith("Basic"));
        }
    }

    @Test
    void testHTTPClientConfiguration() throws IOException, InterruptedException {
        mockWebServer.enqueue(new MockResponse()
                .setBody(mockResponseJson)
                .setHeader("Content-Type", "application/json"));

        JSONObject jsonObject = new JSONObject();
        jsonObject.put("source", "universal");
        jsonObject.put("url", "https://www.macys.com/shop/sale");

        MediaType mediaType = MediaType.parse("application/json; charset=utf-8");
        RequestBody body = RequestBody.create(jsonObject.toString(), mediaType);
        Request request = new Request.Builder()
                .url(mockWebServer.url("/v1/queries"))
                .post(body)
                .build();

        try (Response response = client.newCall(request).execute()) {
            assertEquals(200, response.code());
            
            RecordedRequest recordedRequest = mockWebServer.takeRequest();
            assertEquals("POST", recordedRequest.getMethod());
            assertEquals("/v1/queries", recordedRequest.getPath());
            assertTrue(recordedRequest.getHeader("Content-Type").contains("application/json"));
            
            String requestBody = recordedRequest.getBody().readUtf8();
            JSONObject requestJson = new JSONObject(requestBody);
            assertEquals("universal", requestJson.getString("source"));
            assertTrue(requestJson.getString("url").contains("macys.com"));
        }
    }

    @Test
    void testAPIResponseStructure() throws IOException {
        mockWebServer.enqueue(new MockResponse()
                .setBody(mockResponseJson)
                .setHeader("Content-Type", "application/json"));

        JSONObject jsonObject = new JSONObject();
        jsonObject.put("source", "universal");
        jsonObject.put("url", "https://www.macys.com/shop/sale");

        MediaType mediaType = MediaType.parse("application/json; charset=utf-8");
        RequestBody body = RequestBody.create(jsonObject.toString(), mediaType);
        Request request = new Request.Builder()
                .url(mockWebServer.url("/v1/queries"))
                .post(body)
                .build();

        try (Response response = client.newCall(request).execute()) {
            String responseBody = response.body().string();
            
            ObjectMapper mapper = new ObjectMapper();
            JsonNode jsonNode = mapper.readTree(responseBody);
            
            assertTrue(jsonNode.has("results"));
            assertTrue(jsonNode.get("results").isArray());
            assertTrue(jsonNode.get("results").size() > 0);
            
            JsonNode firstResult = jsonNode.get("results").get(0);
            assertTrue(firstResult.has("content"));
            assertTrue(firstResult.has("job_id"));
            assertTrue(firstResult.has("status_code"));
            assertEquals(200, firstResult.get("status_code").asInt());
        }
    }
}
