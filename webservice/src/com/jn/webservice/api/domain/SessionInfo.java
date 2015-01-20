package com.jn.webservice.api.domain;

import javax.persistence.Column;
import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.GenerationType;
import javax.persistence.Id;
import javax.persistence.Table;

@Entity
@Table(name="sessions")
public class SessionInfo {
	
	@Id
	@GeneratedValue(strategy=GenerationType.AUTO)
	@Column(name="id")
	private Integer id;

	@Column(name="session_id")
	private String sessionID;

	@Column(name="user_id")
	private Integer userID;

	@Column(name="openwebkey")
	private String openWebKey;
	
	@Column(name="openvpnkey")
	private String openVpnKey;

	@Column(name="openvpncert")
	private String openVpnCert;

	@Column(name="starttime")
	private String startTime;
	

	public Integer getId() {
		return id;
	}

	public void setId(Integer id) {
		this.id = id;
	}

	public String getSessionID() {
		return sessionID;
	}

	public void setSessionID(String sessionID) {
		this.sessionID = sessionID;
	}

	public Integer getUserID() {
		return userID;
	}

	public void setUserID(Integer userID) {
		this.userID = userID;
	}

	public String getOpenWebKey() {
		return openWebKey;
	}

	public String getOpenVpnCert() {
		return openVpnCert;
	}

	public void setOpenVpnCert(String openVpnCert) {
		this.openVpnCert = openVpnCert;
	}

	public void setOpenWebKey(String openWebKey) {
		this.openWebKey = openWebKey;
	}

	public String getOpenVpnKey() {
		return openVpnKey;
	}

	public void setOpenVpnKey(String openVpnKey) {
		this.openVpnKey = openVpnKey;
	}

	public String getStartTime() {
		return startTime;
	}

	public void setStartTime(String startTime) {
		this.startTime = startTime;
	}
	
}
