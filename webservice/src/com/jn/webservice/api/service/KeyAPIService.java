package com.jn.webservice.api.service;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.StringReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.URLConnection;

import javax.net.ssl.HostnameVerifier;
import javax.net.ssl.HttpsURLConnection;
import javax.net.ssl.SSLSession;
import javax.servlet.http.HttpServletResponse;
import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;

import org.apache.commons.codec.digest.DigestUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;
import org.springframework.stereotype.Controller;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.w3c.dom.NodeList;
import org.xml.sax.InputSource;

import com.jn.webservice.api.Constants;
import com.jn.webservice.api.KeyGenerate;
import com.jn.webservice.api.Utils;
import com.jn.webservice.api.dao.ServerDAO;
import com.jn.webservice.api.dao.SessionDAO;
import com.jn.webservice.api.dao.UserDAO;
import com.jn.webservice.api.domain.ServerInfo;
import com.jn.webservice.api.domain.SessionInfo;

@Transactional
@Controller
@RequestMapping("Key/")
@Component("KeyAPIService")
public class KeyAPIService extends com.jn.webservice.api.Controller {

	@Autowired
	UserDAO userDao;

	@Autowired
	SessionDAO sessionDao;

	@Autowired
	ServerDAO serverDao;

	static {
		HttpsURLConnection.setDefaultHostnameVerifier(new HostnameVerifier() {
			@Override
			public boolean verify(String arg0, SSLSession arg1) {
				// TODO Auto-generated method stub
				return true;
			}
		});
	};

	@RequestMapping(value = "GetOpenWebKeyClient", produces = "text/xml;charset=UTF-8")
	public void GetOpenWebKeyClient(
			HttpServletResponse res,
			@RequestParam(value = "session_id", required = false) String sessionID) {

		sessionID = Utils.checkNull(sessionID);

		String str = "";
		try {
			Document doc = getDocument();
			if (doc == null) {
				str = getResponseElement(Constants.ResponseStatus.BAD_CERTIFICATION);

			} else {
				Element root = getRootElement(doc);

				Element status;

				try {
					if (sessionID.length() == 0) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.INVALID_SESSION);

					} else {

						try {
							SessionInfo session = sessionDao
									.getSessionInfo(sessionID);

							if (session == null) {
								status = getResponseElement(
										doc,
										Constants.ResponseStatus.INVALID_SESSION);

							} else {

								int diff = Utils.getDifferentTimes(
										Utils.getTodayWithTime(),
										session.getStartTime());

								if (diff > Constants.EXPIRED_SESSION_TIME) {
									status = getResponseElement(
											doc,
											Constants.ResponseStatus.EXPIRED_SESSION);

								} else {

									String openWebKey = Utils.getKey(8);
									session.setOpenWebKey(openWebKey);

									String err = sessionDao.update(session);
									if (err.length() > 0) {
										status = getResponseElement(doc,
												Constants.ResponseStatus.FAILED);

									} else {
										Element key = getKeyElement(doc, "key",
												openWebKey);
										root.appendChild(key);

										status = getResponseElement(
												doc,
												Constants.ResponseStatus.SUCCESS);
									}
								}
							}
						} catch (Exception fsdfs) {
							status = getResponseElement(doc,
									Constants.ResponseStatus.FAILED);
						}

					}
				} catch (Exception f) {
					status = getResponseElement(doc,
							Constants.ResponseStatus.BAD_REQUEST);
				}

				root.appendChild(status);

				doc.appendChild(root);
				str = makeXMLString(doc);
			}

			print(res, str);
		} catch (Exception e) {
		}
	}

	@RequestMapping(value = "GetOpenWebKeyServer", produces = "text/xml;charset=UTF-8")
	public void GetOpenWebKeyServer(
			HttpServletResponse res,
			@RequestParam(value = "server_id", required = false) String serverID,
			@RequestParam(value = "password", required = false) String password,
			@RequestParam(value = "session_id", required = false) String sessionID) {

		sessionID = Utils.checkNull(sessionID);
		serverID = Utils.checkNull(serverID);
		password = Utils.checkNull(password);

		String str = "";
		try {
			Document doc = getDocument();
			if (doc == null) {
				str = getResponseElement(Constants.ResponseStatus.BAD_CERTIFICATION);

			} else {
				Element root = getRootElement(doc);

				Element status;

				try {
					if (sessionID.length() == 0) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.INVALID_SESSION);

					} else if (serverID.length() == 0) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.INVALID_SERVER);

					} else if (password.length() == 0) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.INVALID_PASSWORD);

					} else {

						try {
							SessionInfo session = sessionDao
									.getSessionInfo(sessionID);

							if (session == null) {
								status = getResponseElement(
										doc,
										Constants.ResponseStatus.INVALID_SESSION);

							} else {

								int diff = Utils.getDifferentTimes(
										Utils.getTodayWithTime(),
										session.getStartTime());

								if (diff > Constants.EXPIRED_SESSION_TIME) {
									status = getResponseElement(
											doc,
											Constants.ResponseStatus.EXPIRED_SESSION);

								} else {

									ServerInfo server = serverDao
											.getServerInfo(Utils
													.getInt(serverID));
									if (server == null) {
										status = getResponseElement(
												doc,
												Constants.ResponseStatus.INVALID_SERVER);

									} else {

										if (!DigestUtils
												.md5Hex(password
														+ server.getPasswordsalt())
												.equals(server
														.getPasswordhash())) {
											status = getResponseElement(
													doc,
													Constants.ResponseStatus.INVALID_PASSWORD);

										} else {
											String openWebKey = session
													.getOpenWebKey();

											if (openWebKey == null
													|| openWebKey.length() == 0) {
												status = getResponseElement(
														doc,
														Constants.ResponseStatus.NONEXIST_KEY);

											} else {
												Element key = getKeyElement(
														doc, "key", openWebKey);
												root.appendChild(key);

												status = getResponseElement(
														doc,
														Constants.ResponseStatus.SUCCESS);
											}

										}
									}
								}
							}
						} catch (Exception fsdfs) {
							status = getResponseElement(doc,
									Constants.ResponseStatus.FAILED);
						}

					}
				} catch (Exception f) {
					status = getResponseElement(doc,
							Constants.ResponseStatus.BAD_REQUEST);
				}

				root.appendChild(status);

				doc.appendChild(root);
				str = makeXMLString(doc);
			}

			print(res, str);
		} catch (Exception e) {
		}
	}

	@RequestMapping(value = "UploadWebKeyServer", produces = "text/xml;charset=UTF-8")
	public void UploadWebKeyServer(
			HttpServletResponse res,
			@RequestParam(value = "server_id", required = false) String serverID,
			@RequestParam(value = "password", required = false) String password,
			@RequestParam(value = "key", required = false) String key,
			@RequestParam(value = "cert", required = false) String cert,
			@RequestParam(value = "session_id", required = false) String sessionID) {

		sessionID = Utils.checkNull(sessionID);
		serverID = Utils.checkNull(serverID);
		password = Utils.checkNull(password);
		key = Utils.checkNull(key);
		cert = Utils.checkNull(cert);

		String str = "";
		try {
			Document doc = getDocument();
			if (doc == null) {
				str = getResponseElement(Constants.ResponseStatus.BAD_CERTIFICATION);

			} else {
				Element root = getRootElement(doc);

				Element status;

				try {
					if (sessionID.length() == 0) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.INVALID_SESSION);

					} else if (serverID.length() == 0) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.INVALID_SERVER);

					} else if (password.length() == 0) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.INVALID_PASSWORD);

					} else if (key.length() == 0 || key.length() > 3000) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.NONEXIST_KEY);

					} else if (cert.length() == 0 || cert.length() > 3000) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.NONEXIST_CERT);

					} else {

						try {
							SessionInfo session = sessionDao
									.getSessionInfo(sessionID);

							if (session == null) {
								status = getResponseElement(
										doc,
										Constants.ResponseStatus.INVALID_SESSION);

							} else {

								int diff = Utils.getDifferentTimes(
										Utils.getTodayWithTime(),
										session.getStartTime());

								if (diff > Constants.EXPIRED_SESSION_TIME) {
									status = getResponseElement(
											doc,
											Constants.ResponseStatus.EXPIRED_SESSION);

								} else {

									ServerInfo server = serverDao
											.getServerInfo(Utils
													.getInt(serverID));
									if (server == null) {
										status = getResponseElement(
												doc,
												Constants.ResponseStatus.INVALID_SERVER);

									} else {

										if (!DigestUtils
												.md5Hex(password
														+ server.getPasswordsalt())
												.equals(server
														.getPasswordhash())) {
											status = getResponseElement(
													doc,
													Constants.ResponseStatus.INVALID_PASSWORD);

										} else {
											session.setOpenVpnKey(key);
											session.setOpenVpnCert(cert);
											String err = sessionDao
													.update(session);

											if (err.length() > 0) {
												status = getResponseElement(
														doc,
														Constants.ResponseStatus.FAILED);

											} else {
												// Element key =
												// getKeyElement(doc, "key",
												// openWebKey);
												// root.appendChild(key);

												status = getResponseElement(
														doc,
														Constants.ResponseStatus.SUCCESS);
											}
										}
									}
								}
							}
						} catch (Exception fsdfs) {
							status = getResponseElement(doc,
									Constants.ResponseStatus.FAILED);
						}

					}
				} catch (Exception f) {
					status = getResponseElement(doc,
							Constants.ResponseStatus.BAD_REQUEST);
				}

				root.appendChild(status);

				doc.appendChild(root);
				str = makeXMLString(doc);
			}

			print(res, str);
		} catch (Exception e) {
		}
	}

	@RequestMapping(value = "GetOpenVpnKey", produces = "text/xml;charset=UTF-8")
	public void GetOpenVpnKey(
			HttpServletResponse res,
			@RequestParam(value = "server_id", required = false) String serverID,
			@RequestParam(value = "password", required = false) String password,
			@RequestParam(value = "session_id", required = false) String sessionID) {

		sessionID = Utils.checkNull(sessionID);
		serverID = Utils.checkNull(serverID);
		password = Utils.checkNull(password);

		String str = "";
		try {
			Document doc = getDocument();
			if (doc == null) {
				str = getResponseElement(Constants.ResponseStatus.BAD_CERTIFICATION);

			} else {
				Element root = getRootElement(doc);

				Element status;

				try {
					if (sessionID.length() == 0) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.INVALID_SESSION);

					} else if (serverID.length() == 0) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.INVALID_SERVER);

					} else if (password.length() == 0) {
						status = getResponseElement(doc,
								Constants.ResponseStatus.INVALID_PASSWORD);

					} else {

						try {
							SessionInfo session = sessionDao
									.getSessionInfo(sessionID);

							if (session == null) {
								status = getResponseElement(
										doc,
										Constants.ResponseStatus.INVALID_SESSION);

							} else {

								int diff = Utils.getDifferentTimes(
										Utils.getTodayWithTime(),
										session.getStartTime());

								if (diff > Constants.EXPIRED_SESSION_TIME) {
									status = getResponseElement(
											doc,
											Constants.ResponseStatus.EXPIRED_SESSION);

								} else {

									ServerInfo server = serverDao
											.getServerInfo(Utils
													.getInt(serverID));
									if (server == null) {
										status = getResponseElement(
												doc,
												Constants.ResponseStatus.INVALID_SERVER);

									} else {

										if (!DigestUtils
												.md5Hex(password
														+ server.getPasswordsalt())
												.equals(server
														.getPasswordhash())) {
											status = getResponseElement(
													doc,
													Constants.ResponseStatus.INVALID_PASSWORD);

										} else {
											String openWebKey = session
													.getOpenVpnKey();
											String openWebCert = session
													.getOpenVpnCert();

											if (openWebKey == null
													|| openWebKey.length() == 0) {
												status = getResponseElement(
														doc,
														Constants.ResponseStatus.NONEXIST_KEY);

											} else if (openWebCert == null
													|| openWebCert.length() == 0) {
												status = getResponseElement(
														doc,
														Constants.ResponseStatus.NONEXIST_CERT);

											} else {
												Element key = getKeyElement(
														doc, "key", openWebKey);
												root.appendChild(key);

												Element cert = getKeyElement(
														doc, "cert",
														openWebCert);
												root.appendChild(cert);

												status = getResponseElement(
														doc,
														Constants.ResponseStatus.SUCCESS);
											}

										}
									}
								}
							}
						} catch (Exception fsdfs) {
							status = getResponseElement(doc,
									Constants.ResponseStatus.FAILED);
						}

					}
				} catch (Exception f) {
					status = getResponseElement(doc,
							Constants.ResponseStatus.BAD_REQUEST);
				}

				root.appendChild(status);

				doc.appendChild(root);
				str = makeXMLString(doc);
			}

			print(res, str);
		} catch (Exception e) {
		}
	}

	@RequestMapping(value = "GenerateOpenVpnKey", produces = "text/xml;charset=UTF-8")
	public void GenerateOpenVpnKey(
			HttpServletResponse res,
			@RequestParam(value = "method", required = false) String method,
			@RequestParam(value = "server_id", required = false) String serverID,
			@RequestParam(value = "password", required = false) String password,
			@RequestParam(value = "session_id", required = false) String sessionID) {

		method = Utils.checkNull(method);
		sessionID = Utils.checkNull(sessionID);
		serverID = Utils.checkNull(serverID);
		password = Utils.checkNull(password);

		String str = "";
		try {
			Document doc = getDocument();
			if (doc == null) {
				str = getResponseElement(Constants.ResponseStatus.BAD_CERTIFICATION);

			} else {
				Element root = getRootElement(doc);

				Element status;

				try {
					if (method.equals("server")) {

						if (sessionID.length() == 0) {
							status = getResponseElement(doc,
									Constants.ResponseStatus.INVALID_SESSION);

						} else {
							KeyGenerate key = new KeyGenerate();
							key.execute(sessionID);

							String ca_crt = key.getKey("ca.crt");
							if (ca_crt.length() == 0) {
								status = getResponseElement(doc,
										Constants.ResponseStatus.FAILED);

							} else {
								String ca_key = key.getKey("ca.key");

								if (ca_key.length() == 0) {
									status = getResponseElement(doc,
											Constants.ResponseStatus.FAILED);

								} else {
									ca_crt = "<!--" + ca_crt + "-->";
									ca_key = "<!--" + ca_key + "-->";

									Element e_crt = getKeyElement(doc,
											"ca_crt", ca_crt);
									root.appendChild(e_crt);

									Element e_key = getKeyElement(doc,
											"ca_key", ca_key);
									root.appendChild(e_key);

									status = getResponseElement(doc,
											Constants.ResponseStatus.SUCCESS);
								}
							}
						}

					} else {

						if (sessionID.length() == 0) {
							status = getResponseElement(doc,
									Constants.ResponseStatus.INVALID_SESSION);

						} else if (serverID.length() == 0) {
							status = getResponseElement(doc,
									Constants.ResponseStatus.INVALID_SERVER);

						} else if (password.length() == 0) {
							status = getResponseElement(doc,
									Constants.ResponseStatus.INVALID_PASSWORD);

						} else {

							try {
								SessionInfo session = sessionDao
										.getSessionInfo(sessionID);

								if (session == null) {
									status = getResponseElement(
											doc,
											Constants.ResponseStatus.INVALID_SESSION);

								} else {

									int diff = Utils.getDifferentTimes(
											Utils.getTodayWithTime(),
											session.getStartTime());

									if (diff > Constants.EXPIRED_SESSION_TIME) {
										status = getResponseElement(
												doc,
												Constants.ResponseStatus.EXPIRED_SESSION);

									} else {

										ServerInfo server = serverDao
												.getServerInfo(Utils
														.getInt(serverID));
										if (server == null) {
											status = getResponseElement(
													doc,
													Constants.ResponseStatus.INVALID_SERVER);

										} else {

											if (!DigestUtils
													.md5Hex(password
															+ server.getPasswordsalt())
													.equals(server
															.getPasswordhash())) {
												status = getResponseElement(
														doc,
														Constants.ResponseStatus.INVALID_PASSWORD);

											} else {
												String httpsURL = "https://" + server.getAddress() + ":" + Constants.SERVER_PORT + "/o_api/Key/GenerateOpenVpnKey?session_id=" + sessionID + "&method=server";

												try {
													URL myurl = new URL(
															httpsURL);
													HttpsURLConnection con = (HttpsURLConnection) myurl
															.openConnection();
													InputStream ins = con
															.getInputStream();
													InputStreamReader isr = new InputStreamReader(
															ins);
													BufferedReader in = new BufferedReader(
															isr);

													String response_str = "";
													String inputLine;

													while ((inputLine = in
															.readLine()) != null) {
														response_str += inputLine;
													}

													in.close();

													if (response_str.length()==0){
														status = getResponseElement(
																doc,
																Constants.ResponseStatus.FAILED);
													}else{
//														System.out.println("Response :" + response_str);
														
														try{
														    DocumentBuilder db = DocumentBuilderFactory.newInstance().newDocumentBuilder();
														    InputSource is = new InputSource();
														    is.setCharacterStream(new StringReader(response_str));

														    Document response_doc = db.parse(is);
														    NodeList response_status_list = response_doc.getElementsByTagName("response");
														    if (response_status_list.getLength()!=1)
														    	throw new Exception("");
														    
														    Element response_status = (Element)response_status_list.item(0);
														    NodeList code_list= response_status.getElementsByTagName("code");
														    Element code = (Element) code_list.item(0);

//														    System.out.println("Response Code :" + code.getNodeName() + "," + code.getNodeType() + "," + code.getChildNodes().item(0).getNodeValue());
														    String response_code= code.getChildNodes().item(0).getNodeValue();
														    if (!response_code.equals("0"))
														    	throw new Exception("");
														    
														    NodeList response_crt_list = response_doc.getElementsByTagName("ca_crt");
														    NodeList response_key_list = response_doc.getElementsByTagName("ca_key");
														    
														    String ca_crt = response_crt_list.item(0).getChildNodes().item(0).getNodeValue();
														    String ca_key = response_key_list.item(0).getChildNodes().item(0).getNodeValue();

															Element e_crt = getKeyElement(doc,
																	"ca_crt", ca_crt);
															root.appendChild(e_crt);

															Element e_key = getKeyElement(doc,
																	"ca_key", ca_key);
															root.appendChild(e_key);
														    
															status = getResponseElement(
																	doc,
																	Constants.ResponseStatus.SUCCESS);
															
														}catch (Exception fff){
															status = getResponseElement(
																	doc,
																	Constants.ResponseStatus.FAILED);
														}
													}
													
												} catch (IOException e) {
//													System.err.println(e);
													e.printStackTrace();
													status = getResponseElement(
															doc,
															Constants.ResponseStatus.FAILED);
												}
											}
										}
									}
								}
							} catch (Exception fsdfs) {
								status = getResponseElement(doc,
										Constants.ResponseStatus.FAILED);
							}

						}
					}
				} catch (Exception f) {
					status = getResponseElement(doc,
							Constants.ResponseStatus.BAD_REQUEST);
				}

				root.appendChild(status);

				doc.appendChild(root);
				str = makeXMLString(doc);
			}

			print(res, str);
		} catch (Exception e) {
		}
	}

}
